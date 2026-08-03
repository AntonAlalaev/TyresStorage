// Пин W5500	Пин ESP32	  Примечание
// VCC	      3V3	        Питание 3.3В (ток до 300 мА – ESP32 выдерживает)
// GND	      GND	        Общий
// MOSI	      GPIO23
// MISO	      GPIO19
// SCK	      GPIO18
// CS	        GPIO5	      Можно выбрать любой, но в коде укажем этот
// RST	      GPIO4	      Опционально, для программного сброса. Лучше подключить и управлять
// INT	      не используется

#include <SPI.h>
#include <Ethernet.h>
#include "esp_task_wdt.h"  // для Watchdog

// ===== Управление отладочным выводом =====
bool serialEnabled = true;  // true – вывод в Serial включён (ожидание 15 сек), false – автономно

#define DEBUG_PRINT(x) \
  do { \
    if (serialEnabled) Serial.print(x); \
  } while (0)
#define DEBUG_PRINTLN(x) \
  do { \
    if (serialEnabled) Serial.println(x); \
  } while (0)

// ===== Настройка Watchdog =====
#define ENABLE_WATCHDOG true       // true - включён, false - выключен (для отладки)
#define WATCHDOG_TIMEOUT_MS 10000  // 10 секунд до перезагрузки при зависании loop

// ===== Настройка пинов =====
#define W5500_CS 5
#define W5500_RST 4
#define RELAY_PIN_LED 2
#define RELAY_PIN_REAL 32

#define RELAY_ACTIVE_HIGH false  // реле включается при LOW

// ===== Настройка сети =====
byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED };
IPAddress ip(192, 168, 1, 101);
IPAddress gateway(192, 168, 1, 1);
IPAddress subnet(255, 255, 255, 0);

EthernetServer server(80);

unsigned long relayOnTime = 0;
unsigned long duration = 0;
bool relayActive = false;

// ===== Для периодической проверки Ethernet =====
unsigned long lastEthernetCheck = 0;
const unsigned long ETHERNET_CHECK_INTERVAL = 60000;  // 60 секунд

// ---------- Функция управления реле ----------
void setRelay(bool on) {
  digitalWrite(RELAY_PIN_LED, on ? HIGH : LOW);

  if (on) {
    pinMode(RELAY_PIN_REAL, OUTPUT);
    digitalWrite(RELAY_PIN_REAL, LOW);
  } else {
    pinMode(RELAY_PIN_REAL, INPUT_PULLUP);
  }

  if (serialEnabled) {
    Serial.print("setRelay(");
    Serial.print(on);
    Serial.print(") -> Mode = ");
    Serial.println(on ? "OUTPUT (LOW)" : "INPUT_PULLUP (3.3V)");
  }
}

// ---------- Сброс и переинициализация W5500 ----------
void resetW5500() {
  DEBUG_PRINTLN("=== W5500 RESET START ===");

  digitalWrite(W5500_RST, LOW);
  delay(10);
  digitalWrite(W5500_RST, HIGH);
  delay(100);

  Ethernet.init(W5500_CS);
  Ethernet.begin(mac, ip, gateway, subnet);

  if (Ethernet.hardwareStatus() == EthernetNoHardware) {
    DEBUG_PRINTLN("ERROR: W5500 not available after reset!");
  } else {
    DEBUG_PRINTLN("W5500 reset OK");
  }

  server.begin();
  DEBUG_PRINT("Server restarted IP: ");
  DEBUG_PRINTLN(Ethernet.localIP());
  DEBUG_PRINTLN("=== W5500 RESET END ===");
}

// ---------- Проверка состояния (только аппаратный статус) ----------
bool checkEthernetStatus() {
  if (Ethernet.hardwareStatus() == EthernetNoHardware) {
    DEBUG_PRINTLN("Check: W5500 hardware missing!");
    return false;
  }
  return true;
}

// ---------- setup ----------
void setup() {
  Serial.begin(115200);
  if (serialEnabled) {
    unsigned long startWait = millis();
    while (!Serial && (millis() - startWait < 15000)) {
      // ждём до 15 секунд
    }
    if (!Serial) {
      serialEnabled = false;
    }
  }

  // --- Инициализация Watchdog ---
  if (ENABLE_WATCHDOG) {
    esp_task_wdt_config_t wdt_config = {
      .timeout_ms = WATCHDOG_TIMEOUT_MS,      // теперь в миллисекундах
      .idle_core_mask = (1 << 0) | (1 << 1),  // оба ядра ESP32
      .trigger_panic = true                   // вызывать панику и перезагрузку
    };
    esp_task_wdt_init(&wdt_config);
    esp_task_wdt_add(NULL);  // добавляем текущую задачу (loop)
    DEBUG_PRINT("Watchdog enabled with timeout ");
    DEBUG_PRINT(WATCHDOG_TIMEOUT_MS);
    DEBUG_PRINTLN(" ms");
  } else {
    DEBUG_PRINTLN("Watchdog disabled");
  }

  pinMode(RELAY_PIN_LED, OUTPUT);
  pinMode(RELAY_PIN_REAL, INPUT_PULLUP);
  setRelay(false);

  // Сброс W5500 при старте
  pinMode(W5500_RST, OUTPUT);
  digitalWrite(W5500_RST, LOW);
  delay(10);
  digitalWrite(W5500_RST, HIGH);
  delay(100);

  Ethernet.init(W5500_CS);
  Ethernet.begin(mac, ip, gateway, subnet);

  if (Ethernet.hardwareStatus() == EthernetNoHardware) {
    DEBUG_PRINTLN("Error: W5500 not available!");
    while (true) delay(1);
  }

  server.begin();
  DEBUG_PRINT("Server started IP: ");
  DEBUG_PRINTLN(Ethernet.localIP());

  lastEthernetCheck = millis();
}

// ---------- loop ----------
void loop() {
  // --- Сброс Watchdog (если включён) ---
  if (ENABLE_WATCHDOG) {
    esp_task_wdt_reset();
  }

  // --- Обработка HTTP-клиентов ---
  EthernetClient client = server.available();
  if (client) {
    String request = "";
    while (client.connected()) {
      if (client.available()) {
        char c = client.read();
        request += c;
        if (c == '\n') {
          // === Ручной сброс W5500 ===
          if (request.startsWith("GET /reset")) {
            DEBUG_PRINTLN("Manual reset requested");
            client.println("HTTP/1.1 200 OK");
            client.println("Content-Type: text/plain");
            client.println("Connection: close");
            client.println();
            client.println("OK: resetting W5500...");
            client.stop();
            resetW5500();
            break;
          }
          // === /start?time= ===
          else if (request.startsWith("GET /start?time=")) {
            int timePos = request.indexOf("time=") + 5;
            int endPos = request.indexOf(' ', timePos);
            if (endPos == -1) endPos = request.length();
            String timeStr = request.substring(timePos, endPos);
            long sec = timeStr.toInt();
            if (sec > 0) {
              duration = sec * 1000UL;
              relayOnTime = millis();
              setRelay(true);
              relayActive = true;
              DEBUG_PRINT("Relay ON for ");
              DEBUG_PRINT(sec);
              DEBUG_PRINTLN(" sec");
              client.println("HTTP/1.1 200 OK");
              client.println("Content-Type: text/plain");
              client.println("Connection: close");
              client.println();
              client.print("OK: relay ON ");
              client.print(sec);
              client.println(" sec");
            } else {
              client.println("HTTP/1.1 400 Bad Request");
              client.println("Content-Type: text/plain");
              client.println("Connection: close");
              client.println();
              client.println("Error: time must be >0");
            }
          }
          // === /stop ===
          else if (request.startsWith("GET /stop")) {
            setRelay(false);
            relayActive = false;
            DEBUG_PRINTLN("Relay OFF by /stop");
            client.println("HTTP/1.1 200 OK");
            client.println("Content-Type: text/plain");
            client.println("Connection: close");
            client.println();
            client.println("OK: relay OFF");
          }
          // === HTML-страница ===
          else {
            client.println("HTTP/1.1 200 OK");
            client.println("Content-Type: text/html");
            client.println("Connection: close");
            client.println();
            client.println("<!DOCTYPE HTML><html><head><meta charset='utf-8'></head>");
            client.println("<h2>Управление реле</h2>");
            client.println("<form action='/start' method='get'>");
            client.println("Время (сек): <input type='number' name='time' value='10' min='1'>");
            client.println("<input type='submit' value='Запустить'>");
            client.println("</form>");
            client.println("<form action='/stop' method='get'>");
            client.println("<input type='submit' value='Выключить'>");
            client.println("</form>");
            client.println("<form action='/reset' method='get'>");
            client.println("<input type='submit' value='Сбросить W5500'>");
            client.println("</form>");
            if (relayActive) {
              unsigned long elapsed = (millis() - relayOnTime) / 1000;
              unsigned long remaining = duration / 1000 - elapsed;
              client.print("<p>Реле активно, осталось ");
              client.print(remaining);
              client.println(" сек</p>");
            } else {
              client.println("<p>Реле выключено</p>");
            }
            client.println("</html>");
          }
          break;
        }
      }
    }
    delay(1);
    client.stop();
  }

  // --- Таймер реле ---
  if (relayActive) {
    if (millis() - relayOnTime >= duration) {
      setRelay(false);
      relayActive = false;
      DEBUG_PRINTLN("Relay OFF by timeout");
    }
  }

  // --- Периодическая проверка Ethernet (только аппаратный статус) ---
  if (millis() - lastEthernetCheck >= ETHERNET_CHECK_INTERVAL) {
    lastEthernetCheck = millis();
    if (!checkEthernetStatus()) {
      DEBUG_PRINTLN("Ethernet hardware problem detected, performing auto reset...");
      resetW5500();
    }
  }
}
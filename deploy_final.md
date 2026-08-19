

# 📋 Финальные шаги

## 1. Создать systemd-службу для приложения

Создай файл `/etc/systemd/system/tyresstorage.service`:

```bash
sudo nano /etc/systemd/system/tyresstorage.service
```

Содержимое:

```ini
[Unit]
Description=TyresStorage Device Manager
After=network.target

[Service]
WorkingDirectory=/home/pi/TyresStorage
ExecStart=/home/pi/TyresStorage/TyresStorage
Restart=always
RestartSec=10
User=pi
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
```

Сохрани (`Ctrl+O`, `Enter`, `Ctrl+X`).

Включи и запусти:

```bash
sudo systemctl enable tyresstorage.service
sudo systemctl start tyresstorage.service
```

Проверь статус:

```bash
sudo systemctl status tyresstorage.service
```

Должно быть `active (running)`.

---

## 2. Настроить автологин в систему

Включи автоматический вход в графическую оболочку:

```bash
sudo raspi-config
```

- Выбери **3 Boot Options** → **B1 Desktop / CLI** → **B4 Desktop Autologin**.
- Нажми **Finish** и согласись на перезагрузку (пока можно отказаться).

---

## 3. Создать скрипт для запуска Chromium в киоск-режиме

Создай файл `/home/pi/kiosk.sh`:

```bash
nano /home/pi/kiosk.sh
```

Содержимое:

```bash
#!/bin/bash
xset s noblank
xset s off
xset -dpms
unclutter -idle 0.5 -root &

# Очистка кэша Chromium, чтобы не было ошибок восстановления
sed -i 's/"exited_cleanly":false/"exited_cleanly":true/' /home/pi/.config/chromium/Default/Preferences
sed -i 's/"exit_type":"Crashed"/"exit_type":"Normal"/' /home/pi/.config/chromium/Default/Preferences

/usr/bin/chromium-browser --noerrdialogs --disable-infobars --kiosk http://localhost:5000
```

Сделай исполняемым:

```bash
chmod +x /home/pi/kiosk.sh
```

---

## 4. Создать systemd-службу для браузера

Создай файл `/etc/systemd/system/kiosk.service`:

```bash
sudo nano /etc/systemd/system/kiosk.service
```

Содержимое:

```ini
[Unit]
Description=Chromium Kiosk
Wants=graphical.target
After=graphical.target

[Service]
Environment=DISPLAY=:0
Environment=XAUTHORITY=/home/pi/.Xauthority
Type=simple
ExecStart=/bin/bash /home/pi/kiosk.sh
Restart=on-abort
User=pi
Group=pi

[Install]
WantedBy=graphical.target
```

Включи и запусти:

```bash
sudo systemctl enable kiosk.service
sudo systemctl start kiosk.service
```

Проверь статус:

```bash
sudo systemctl status kiosk.service
```

---

## 5. Проверь, что `unclutter` установлен (для скрытия курсора)

Если нет:

```bash
sudo apt install unclutter
```

---

## 6. Перезагрузка и финальный тест

```bash
sudo reboot
```

После перезагрузки должно произойти:

- Система залогинится автоматически.
- Запустится служба `tyresstorage.service` (приложение).
- Запустится служба `kiosk.service` (Chromium в полноэкранном режиме с `http://localhost:5000`).

---

## 🧪 Проверка

- Если браузер не открывается, проверь логи службы:

```bash
sudo journalctl -u kiosk.service -f
```

- Если приложение не стартует:

```bash
sudo journalctl -u tyresstorage.service -f
```

Если всё работает, проект готов.

```

```

---

Скопируй этот текст в любой текстовый редактор и сохрани с расширением `.md` – форматирование сохранится.

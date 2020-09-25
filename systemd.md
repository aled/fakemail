# htmlgenerator unit file

unit file: /etc/systemd/system/htmlgenerator.service

[Unit]
Description=Html Generator service.

[Service]
Type=simple
ExecStart=/opt/dotnet-3.1.5/dotnet /opt/fakemail/HtmlGenerator/HtmlGenerator.dll
Restart=always
RestartSec=30

[Install]
WantedBy=multi-user.target


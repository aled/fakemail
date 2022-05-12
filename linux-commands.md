rebuild opensmtpd with message-id patch:
    # https://www.linuxfordevices.com/tutorials/debian/build-packages-from-source
    # https://github.com/OpenSMTPD/OpenSMTPD/issues/1068

watch for incoming mail:
    # inotifywait -m /home/fakemail/mail/new

list of sqlite tables:
    # sqlite3
    > SELECT name, sql FROM sqlite_master WHERE type='table' ORDER BY name;


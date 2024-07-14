rebuild opensmtpd with message-id patch:
    # https://www.linuxfordevices.com/tutorials/debian/build-packages-from-source
    # https://github.com/OpenSMTPD/OpenSMTPD/issues/1068

    # apt-get install dpkg-dev
    # apt-get source opensmtpd
    # cd opensmtpd-6.8.0p2/
    # nano usr.sbin/smtpd/smtp_session.c    
       -    tx->session->listener->port == 587) {
       +    tx->session->listener->port == htons(587)) {
    # apt build-dep opensmtpd
    # dpkg-buildpackage -b -uc -us
    # cd ..
    # dpkg -i opensmtpd_6.8.0p2-4_amd64.deb

watch for incoming mail:
    # inotifywait -m /home/fakemail/mail/new

list of sqlite tables:
    # sqlite3
    > SELECT name, sql FROM sqlite_master WHERE type='table' ORDER BY name;


watch for incoming mail:
    # inotifywait -m /home/fakemail/mail/new

list of sqlite tables:
    # sqlite3
    > SELECT name, sql FROM sqlite_master WHERE type='table' ORDER BY name;


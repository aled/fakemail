BEGIN TRANSACTION;

ALTER TABLE "User" RENAME COLUMN "LastUpdatedTimestamp" TO "UpdatedTimestampUtc";

ALTER TABLE "User" RENAME COLUMN "InsertedTimestamp" TO "CreatedTimestampUtc";

ALTER TABLE "SmtpUser" RENAME COLUMN "LastUpdatedTimestamp" TO "UpdatedTimestampUtc";

ALTER TABLE "SmtpUser" RENAME COLUMN "InsertedTimestamp" TO "CreatedTimestampUtc";

ALTER TABLE "Email" RENAME COLUMN "ReceivedTimestamp" TO "ReceivedTimestampUtc";

CREATE TABLE "ef_temp_User" (
    "UserId" TEXT NOT NULL CONSTRAINT "PK_User" PRIMARY KEY,
    "CreatedTimestampUtc" TEXT NOT NULL,
    "IsAdmin" INTEGER NOT NULL,
    "PasswordCrypt" TEXT NOT NULL,
    "UpdatedTimestampUtc" TEXT NOT NULL,
    "Username" TEXT NOT NULL
);

INSERT INTO "ef_temp_User" ("UserId", "CreatedTimestampUtc", "IsAdmin", "PasswordCrypt", "UpdatedTimestampUtc", "Username")
SELECT "UserId", "CreatedTimestampUtc", "IsAdmin", "PasswordCrypt", "UpdatedTimestampUtc", "Username"
FROM "User";

CREATE TABLE "ef_temp_SmtpUser" (
    "SmtpUsername" TEXT NOT NULL CONSTRAINT "PK_SmtpUser" PRIMARY KEY,
    "CreatedTimestampUtc" TEXT NOT NULL,
    "SmtpPasswordCrypt" TEXT NOT NULL,
    "UpdatedTimestampUtc" TEXT NOT NULL,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "FK_SmtpUser_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("UserId") ON DELETE CASCADE
);

INSERT INTO "ef_temp_SmtpUser" ("SmtpUsername", "CreatedTimestampUtc", "SmtpPasswordCrypt", "UpdatedTimestampUtc", "UserId")
SELECT "SmtpUsername", "CreatedTimestampUtc", "SmtpPasswordCrypt", "UpdatedTimestampUtc", "UserId"
FROM "SmtpUser";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "User";

ALTER TABLE "ef_temp_User" RENAME TO "User";

DROP TABLE "SmtpUser";

ALTER TABLE "ef_temp_SmtpUser" RENAME TO "SmtpUser";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE UNIQUE INDEX "IX_User_Username" ON "User" ("Username");

CREATE INDEX "IX_SmtpUser_UserId" ON "SmtpUser" ("UserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220508130133_ChangeDateTimeOffsetToDateTime', '6.0.4');

COMMIT;



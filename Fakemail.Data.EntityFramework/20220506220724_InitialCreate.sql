CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "SmtpAlias" (
    "Account" TEXT NOT NULL CONSTRAINT "PK_SmtpAlias" PRIMARY KEY
);

CREATE TABLE "User" (
    "UserId" TEXT NOT NULL CONSTRAINT "PK_User" PRIMARY KEY,
    "Username" TEXT NOT NULL,
    "PasswordCrypt" TEXT NOT NULL,
    "IsAdmin" INTEGER NOT NULL,
    "InsertedTimestamp" TEXT NOT NULL,
    "LastUpdatedTimestamp" TEXT NOT NULL,
    "DeletedTimestamp" TEXT NOT NULL
);

CREATE TABLE "SmtpUser" (
    "SmtpUsername" TEXT NOT NULL CONSTRAINT "PK_SmtpUser" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "SmtpPasswordCrypt" TEXT NOT NULL,
    "InsertedTimestamp" TEXT NOT NULL,
    "LastUpdatedTimestamp" TEXT NOT NULL,
    "DeletedTimestamp" TEXT NOT NULL,
    CONSTRAINT "FK_SmtpUser_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("UserId") ON DELETE CASCADE
);

CREATE TABLE "Email" (
    "EmailId" TEXT NOT NULL CONSTRAINT "PK_Email" PRIMARY KEY,
    "MimeMessage" BLOB NOT NULL,
    "From" TEXT NOT NULL,
    "To" TEXT NOT NULL,
    "CC" TEXT NOT NULL,
    "DeliveredTo" TEXT NOT NULL,
    "Subject" TEXT NOT NULL,
    "ReceivedFromHost" TEXT NOT NULL,
    "ReceivedFromDns" TEXT NOT NULL,
    "ReceivedFromIp" TEXT NOT NULL,
    "ReceivedSmtpId" TEXT NOT NULL,
    "ReceivedTlsInfo" TEXT NOT NULL,
    "ReceivedTimestamp" TEXT NOT NULL,
    "BodyLength" INTEGER NOT NULL,
    "BodySummary" TEXT NOT NULL,
    "BodyChecksum" INTEGER NOT NULL,
    "SmtpUsername" TEXT NULL,
    CONSTRAINT "FK_Email_SmtpUser_SmtpUsername" FOREIGN KEY ("SmtpUsername") REFERENCES "SmtpUser" ("SmtpUsername")
);

CREATE TABLE "Attachment" (
    "AttachmentId" TEXT NOT NULL CONSTRAINT "PK_Attachment" PRIMARY KEY,
    "EmailId" TEXT NOT NULL,
    "Filename" TEXT NOT NULL,
    "ContentType" TEXT NOT NULL,
    "Content" BLOB NOT NULL,
    "ContentChecksum" INTEGER NOT NULL,
    CONSTRAINT "FK_Attachment_Email_EmailId" FOREIGN KEY ("EmailId") REFERENCES "Email" ("EmailId") ON DELETE CASCADE
);

INSERT INTO "SmtpAlias" ("Account")
VALUES ('fakemail');

CREATE INDEX "IX_Attachment_EmailId" ON "Attachment" ("EmailId");

CREATE INDEX "IX_Email_SmtpUsername" ON "Email" ("SmtpUsername");

CREATE INDEX "IX_SmtpUser_UserId" ON "SmtpUser" ("UserId");

CREATE UNIQUE INDEX "IX_User_Username" ON "User" ("Username");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20220506220724_InitialCreate', '6.0.4');

COMMIT;



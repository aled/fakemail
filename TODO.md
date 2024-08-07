# to-do list

Immediate:
- Add curl command lines to web page
- handle errors in front end (e.g. too many requests, SMTP send error)
- Update ansible (sqlite files need to be group-writable)
- add version number to UI
- incrementing build number?

Next version:
- long-polling streaming of new emails to api client
- store emails and attachments using hash-of-content (or messageid) as the key
  (need to remove delivered-to header)
- Incorporate the delivery agent into the API?
- Add metrics/logs

Future:
- Use MimeMessage to remove Delivered-To/MessageId and re-serialize?
- private addresses - no auth - "use this address xxx@fakemail.stream"
- webhooks
- Dockerize everything
- Testing/CI/CD
- Websocket streaming of new data to frontend
- Metrics/stats/monitoring
- Allow authentication (oauth - facebook, google, github, ms, apple etc)
- Allow subdomains of fakemail.stream.

Database/API:

Done
- Automated deployment
- auto delete after 15 minutes
- deploy to production from github
- deploy to dev from github
- don't deploy if somthing fails in github actions
- Set up systemd to run HtmlGenerator as a service
- Keep mail longer than 15 mins? Keep last 100 mails instead?
- SSL for website (nginx reverse proxy, letsencrypt)
- logging framework (serilog?)
- Custom SMTP server (no postfix?)
- command-line program (like wttr.in)
- check if incoming address is valid
- Use SMTP auth for user Id
- Use database (postgresql?) for SMTP auth
- Implement GetEmails API
- Implement RenewToken API
- Turn off tailscale VPN on public server
- Write MDA program to insert email into database - use filesystemwatcher in C#,
  running as a service in the API project
- SSL for SMTP
- Switch to sha256 for shorter tokens
- store plaintext smtp passwords
- No hardcoding - use appsettings.json and appsettings.development.json
- Investigate - does API automatically apply migrations?
- Why do failed deliveries get deleted?
- change to not show all mails - have a unique id per user.
- Responsive static website (bootstrap). Allow toggle of auto-refresh.
- Asp.net website (blazor webassembly? svelte? react?)
- download as .EML file
- use github secrets for JWT keys, SSH keys
- ansible deployment
- github actions

# to-do list

Immediate:
- auto delete after 15 minutes
- download as .EML file
- use github secrets for JWT keys, SSH keys
- ansible deployment
- github actions

Next version:
- store emails and attachments using hash-of-content (or messageid) as the key
  (need to remove delivered-to header)
- Incorporate the delivery agent into the API?
- Automated deployment
- Add metrics/logs

Future:
- Use MimeMessage to remove Delivered-To/MessageId and re-serialize?
- private addresses - no auth - "use this address xxx@fakemail.stream"
- webhooks
- Dockerize everything
- Testing/CI/CD
- Websocket streaming of new data
- Metrics/stats/monitoring
- Allow authentication (oauth - facebook, google, github, ms, apple etc)
- Allow subdomains of fakemail.stream.

Database/API:

Done
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

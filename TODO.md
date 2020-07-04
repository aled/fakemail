Done
- Set up systemd to run HtmlGenerator as a service
- Keep mail longer than 15 mins? Keep last 100 mails instead?
- SSL for website (nginx reverse proxy, letsencrypt)

Immediate:
- logging framework (serilog?)
- CSS for full mail page

Next version:
- Switch to private addresses
- Use a database to enable searching/filtering/authorization (pgsql?)
- Initial page: still public - "public@fakemail.stream"
- private addresses - no auth - "use this address xxx@fakemail.stream"

Future:
- change to not show all mails - have a unique id per user.
- webhooks
- Dockerize everything
- SSL for SMTP
- Custom SMTP server (no postfix?)
- Testing/CI/CD
- Responsive static website (bootstrap). Allow toggle of auto-refresh.
- Asp.net website (blazor webassembly? svelte? react?)
- Websocket streaming of new data
- Metrics/stats/monitoring
- Allow authentication (oauth - facebook, google, github, ms, apple etc)
- Allow subdomains of fakemail.stream.
- command-line program (like wttr.in)


Database/API:
- check if incoming address is valid


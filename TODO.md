# to-do list

Immediate:
- Delete redis. Use Sqlite/EF Core.
- Use SMTP auth for user Id

- Add production https certificate for gRPC
- automated deployment

Next version:
- Switch to private addresses
- Use a database to enable searching/filtering/authorization (pgsql?)
- Initial page: still public - "public@fakemail.stream"
- CSS for full mail page

Future:
- change to not show all mails - have a unique id per user.
- webhooks
- Dockerize everything
- SSL for SMTP
- Testing/CI/CD
- Responsive static website (bootstrap). Allow toggle of auto-refresh.
- Asp.net website (blazor webassembly? svelte? react?)
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
- private addresses - no auth - "use this address xxx@fakemail.stream"


# certbot on nginx

apt install certbot python3-certbot-nginx
certbot --nginx

-- doesn't work.

    sudo apt-get update
    sudo apt-get install software-properties-common
    sudo add-apt-repository universe
    sudo add-apt-repository ppa:certbot/certbot
    sudo apt-get update

need to edit nginx site config:

        listen 443 ssl default_server proxy_protocol;
        listen [::]:443 ssl default_server proxy_protocol;

        ssl_certificate  /etc/letsencrypt/live/www.fakemail.stream/fullchain.pem;
        ssl_certificate_key  /etc/letsencrypt/live/www.fakemail.stream/privkey.pem;
        

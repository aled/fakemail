To run on a VM:

1. Create VM (ubuntu/debian) 
2. Install ssh and ansible.
4. Limit ssh connections to specific IP address of dev workstation
5. Install fakemail_admin ssh public key in root/.ssh/authorized_keys (note Debian 12 does not accept RSA keys by default)

All config from here is done using ansible from WSL.

1. For file permissions to work in WSL, create /etc/wsl.conf and add 
    [automount]
    options = "metadata"
2. Configure ssh to use key-based authentication only (no passwords)
     ansible-playbook -h 
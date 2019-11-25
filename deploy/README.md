# Deployment script

Test this script by running `vagrant up`.
Then edit `/etc/hosts` to give admin.qa.languagedepot.org the IP address 127.0.0.1,
then run `curl -k https://admin.qa.languagedepot.org:8043/api/users` and you should see a JSON result.
(Or if you have wget installed, `wget --no-check-certificate https://admin.qa.languagedepot.org:8043/api/users`)

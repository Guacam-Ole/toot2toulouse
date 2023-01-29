# Toot2Toulouse (t2t)
Use this documentation if you want to host your own t2t-instance. If instead you are using this as a user and have questions asks the maintainer of your instance instead

## Description
Toot2Toulouse is a crossposter from Mastodon-Toots to Twitter-tweets. It automatically posts public contents from the user using two different apps:
1. WebApp: This is used to allow the user to be authenticated to Mastodon and Twitter. change the settings, export data etc.
2. Service: The service does the main work: Retrieving the toots and tweeting them to Twitter.

Toot2Toulouse is a .net 6 application and so runs on Windows and current Linux distros. This document does not describe how to deploy .net applications in general. Others already did that much better than I can.
This document describes what to do on Twitter, on Mastodon and how to confgure and run the application.

## Preparation
You need to register your app to the [Twitter api](https://developer.twitter.com/en/docs/twitter-api). You also need to register an app to Mastodon(Settings/Development). Twitter can be a bit cumbersome as it will be validated. 
Also what limitation you have on Twitter can be a bit random. You will have to configure accordingly to not run into the limitations.

## Installation

### Web Application:
Copy the config.example.json to config.json and start by entering the secrets from Twitter and Mastodon:
```
{
"Secrets": {
"Salt":"JustASalt",
"Twitter": {
      "Consumer": {
        "ApiKey": "",
        "ApiKeySecret": ""
      },
      "Personal": {
        "AccessToken": "",
        "AccessTokenSecret": ""
      }
    },
    "Mastodon": {
      "ClientId": "",
      "ClientSecret": "",
      "AccessToken": ""
    }
  }
```
"Salt" is used for creating userhashes and and contain any combination you can think of. 

You get the other values from the API pages on Twitter and Mastodon. The Twitter secrets are needed to post to Twitter. 
The Mastodon credentials however are *not* used to receive the users toots but for a service account you created. This account posts status messages to the user when something went wrong for example.

The Application that is used to receive the toots from the user is created dynamically because the user can have any instance. But you don't have to care about that. Nothing to configure there.

The other settings can be left alone for now and modified later if needed. 

Deploy the web application and start it. You should now able to register as a normal user. The user will be asked to authenticate with Mastodon and Twitter. 
When the authentication worked correctly your serive account will send a message to the user.

At this point the userdata is stored in the database. Crossposting is not done by the web application but a dedicated service:

### Service
The Service can be installed onto any location on your server. But it will need to access the configfile and databasefile from the web application. These paths are the only things to configure on the service:
```
{
  "database": "<path do your database dir>",
  "config": "<path to your config directory>",
  "log": "<path to the logfile>"
}
```

The service can be used in two ways. If you start it without any parameter it only checks for toots to crosspost *once*. This is meant to be used when you want to run it as cronjob (recommened):
```.\toot2toulouseservice.exe```

The application should log "Sending toots for xx users" within a few seconds and stop once it is finished.

You can also start it with the loop parameter:
```.\toot2toulouseservice.exe loop```


Call this if you don't want to use a cronjob. The application will loop infinitely until it is aborted.

You're done. Toot something and wait for it to appear on Twitter. (On default settings this should take about 5 minutes)













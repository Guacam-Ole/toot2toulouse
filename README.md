# Toot2Toulouse (t2t)
Use this documentation **if you want to host** your own t2t-instance. If instead you are using this as a user and have questions asks the maintainer of your instance instead or read the [user.md] (will follow)

## Description
Toot2Toulouse is a crossposter from Mastodon-Toots to Twitter-tweets. It automatically posts public contents from the user using two different apps:
1. WebApp: This is used to allow the user to be authenticated to Mastodon and Twitter. change the settings, export data etc.
2. Service: The service does the main work: Retrieving the toots and tweeting them to Twitter.

Toot2Toulouse is a .net 6 application and so runs on Windows and current Linux distros. This document does not describe how to deploy .net applications in general. Others already did that much better than I can.
This document describes what to do on Twitter, on Mastodon and how to confgure and run the application.

## Preparation
You need to register your app to the [Twitter api](https://developer.twitter.com/en/docs/twitter-api). You also need to register an app to Mastodon(Settings/Development). Twitter can be a bit cumbersome as it will be validated by human beings (and Twitter changing its policies at random) so better start with this step because this will be the main pitfall. 
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

Deploy the web application and start it. This can be done by entering the following command:
```
dotnet Toot2ToulouseWeb.dll -urls https://0.0.0.0:5000
```

Portnumber and ip might differ and might depend on the system/hoster you have chosen. You now should be able to open the Web application on the specified port and be able to register as a normal user. The user will be asked to authenticate with Mastodon and Twitter. 
When the authentication worked correctly your service account will send a message to user Maston Useraccount.

At this point the userdata is stored in the database. You can change your settings if you like. These are just for you as a user. Server settings are only changed through the config.

Crossposting is not done by the web application but a dedicated service:

### Service
The Service can be installed onto any location on your server. But it will need to access the configfile and databasefile from the web application. So there are only a few things to be configured for the service: ('config.json')
```
{
  "paths": {
      "database": "<absolute path do your database dir>",
      "config": "<absolute path to your config directory>",
      "log": "<absolute path to the logfile>"
  },
  "loglevel":"Information"
}
```

The service can be used in two ways. If you start it without any parameter it only checks for toots to crosspost *once*. This is meant to be used when you want to run it as cronjob (recommened):
```
dotnet toot2toulouseservice.dll
```

The application should start sending toots within a few seconds and stop once it is finished.

You can also start it with the loop parameter:
```
dotnet toot2toulouseservice.dll loop
```


Call this if you don't want to use a cronjob. The application will loop infinitely until it is aborted. It is recommened to only use this to test on a local machine and always use the cron-option if possible.

You're done. Toot something and wait for it to appear on Twitter.



#### Service Commands
The service can not only crosspost to Twitter but also allows additional maintenance tasks:

`upgrade [version?]` - Upgrades the database to the mentioned version. If version is missing to the most current version. Call this if you install a new version but have a database running at a previous version. Only required on Database changes (major Version changes)

`version` - Display application version

`search [user]@[instance] [query]` - search for a query from the user; The number of matches might be limited by the instance

`invite [user]@[instance]` - sents an invite to that user. 

`listids` - List all Ids and corresponding Usernames as well as any block that might be set

`single [userid] [tootid]` - sends a single toot. Is meant for admin-tests and NOT for regular users as updates will not be saved

`block [userid]` - block a single user

`unblock [userid]` - remove block from a single user


## Blocking and Error handling
You can always block a user manually. In addition, automatic blocks will be set if the authentication from Twitter or Mastodon fails (the user has removed the app permission). Because Mastodon has no fixed autherrorcode and the errormessage varies by instance, ANY Mastodon-Error is treated as an auth error.

Both auth-errors will cause a message to be sent to the user. If the user reauthenticates using `/user/register`, these blocks will automatically removed. Manual blocks can also only be removed manually.



### Config description
The t2t-configfile (inside the web project) is the main configuration for both the web and the service. It contains the following main areas:

`Secrets` : Secrets for the app to access Mastodon and Twitter (see above)

`App`: Main App Settings

`Defaults`: Individual user settings

#### App-Settings
"🔮" = That feature is not yet implemented but can already be configured
|Setting|Description|
|-|-|
|Disclaimer|The Disclaimer/AGB that is shown on the Registration page that the user has to accept|
|Ping|Ping other instances (and tell them that this server exists|🔮
|AvailableLanguages|Languages that your instance supports. Allows different HTML-Pages and "messages.xx.json|
|DefaultLanguage|The language to use.|
|ServiceAppSuffix|The Suffix that will added to all messages (e.g. invites) sent by the service|
|Instance|The Instance the Service Account is hosted on|
|Accountname|Accountname (including instance) of the service account|
|ClientName|Name to be displayed on html-pages|
|Url|The Url t2t is hosted|
|Modes|Describes the modes and limits your server is running at|
|Modes.Active|Current Mode. Can be `Active` (anyone can register), `Invite` (only invited users can register), `Closed` (noone can register|
|Modes.AutoInvite|If the number of users reaches this amount the Active Mode automatically switches from `Open` to `Invite`|
|Modes.AutoClose|if the number of users reaches this amount the Active Mode automatically switches to `Closed`|
|Modes.AllowedInstances|Comma-Seperated List of allowed Mastodon-Instances (null=all are allowed)|
|Modes.BlockedInstances|Comma-Seperated List of forbidden Mastodon-Instances (null=none are forbidden)|
|Modes.AllowBots|Allow Mastodon-Bot-Accounts to use this service?|
|Modes.MaxTootsPerDay|Maximum number of daily allowed toots (at the time of registering)|🔮
|Intervals.MinDelay|Minimum delay the user can choose|
|Intervals.MaxDelay|Maximum delay the user can choose|
|AuthFailureDeleteDays|Number of days after which a user account is deleted once an auth-error on Mastodo or Twitter occured|🔮
|MaxImageSize|Maximum Filesize for images in MB|
|MaxMaxGifSize|Maximum Filesize for GIFs in MB|
|MaxVideoSize|Maximum Filesize for videos in MB|
|MinSplitLength|Number of characters after which a tweet is split into a thread even if no space is found|
|DonateTarget|What the user is donating for if he so chooses. Can be `App` or `Instance`|🔮
|Donations|Links to different Donation-Sites (e.g. Github Sponsor, Ko-Fi, etc.) that will be displayed on server page|🔮

#### Usersettings (defaults)
|Setting|Description|
|-|-|
|VisiblitiesToPost|Array of visibilities can be `Public`, `Private`,`NotListed`|
|Delay|Delay before tweeting|
|Appsuffix.Content|Content to add after each tweet|
|Appsuffix.HideIfBreaks|Don't add Suffix if this would add another post in thread|
|LongContent|What to do if toot is longer than allowed Tweetlength. Can be `Thread`, ``, ``|
|LongContentThreadOptions.Prefix|Prefix to add in first entry of thread|
|LongContentThreadOptions.Suffix|Suffix to add on all entries except the last of thread|
|Replacements|List of automatic replacements|
|UseGlobalMentions|Select `@*`- Replacements from others, top|



# Release notes 
Version 1.1 adds additional Configuration options for the user


## New Features
- User can now configure specific "badwords" to disable tweeting for that toots
- Users can now add translations (e.g. "toot"->"tweet")
- Users can also use those translations to hint which person on Mastodon is who on Twitter (e.g. @jeanluc@enterprise.social -> @uss_captain)
- As Fallback search for usertranslations on other users (personal translations always override global translations)
- Switched to Async methods on Database
- Allow to define the Loglevel from service
- More generic response and errorhandling on webapi

## Fixes 
- On logs display accountname instead of displayname
- Fixed an issue where replying to your own non-tweeted (e.g. "private") toot could cause an error 
- Better log-entries on exceptions
- Database optimizations on service


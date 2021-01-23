![GitHub](https://img.shields.io/github/license/ph09nix/TDLibCore)
![Nuget](https://img.shields.io/nuget/v/TDLibCore)
# TDLibCore
 Wellmade .net extension to append functionality and async calls to Telegram Database Library (Tdlib)

## Dependancies
Currently, this library is interactable with Tdlib version 1.7.0
you need following dll files to be able to work with this library :
- tdjson.dll
- Telegram.Td.dll
- libcrypto-1_1-xARC.dll
- zlib1.dll
- libssl-1_1-xARC.dll

## Functions
Name | Description
-------- | -----------
initializeclient | Initializes TDLibCore instance and run new telegram client
Authenticate | Sends input data for authentication purpos if authorization status matches
Dispose | Object disposal
GetMainChatList | Returns a List<Tdapi.Chat> contains authenticated PhoneNumber main chats
GetSuperGroupUsers | Returns a List<Tdapi.User> contains *supergroupid* userslist
GetSuperGroupUsers | Returns a List<Tdapi.User> contains  userslist of group which has *groupidentifier* in title or username
ExecuteCommandAsync | Returns a Responseobject containing asynchronously  runing a Tdlib.Func
AddChatMember | Adds a user in specified super group

## Events
Name | Description
-------- | -----------
OnVerificationCodeNeeded | Event will be runing whenever authorization state is equal to verification code needed
OnVerificationPasswordNeeded | Event will be runing whenever authorization state is equal to verification password needed
OnReady | Event will be runing when connection is successful

you can also add custom events using mainresponsehandlers object ( check example, in example custom event added for UpdateNewMessage )

## How to use ?
you can build this project or easily use following nuget command :
#### Install-Package TDLibCore -Version 1.0.0
Check & Build example folder


### Donation : 18eHqWzdCFMs8pNXiHAwvpDMHmToxFLhMP (BTC)
#### support / suggestion = ph09nixom@gmail.com - t.me/ph09nix
#### Leave a STAR if you found this usefull :)

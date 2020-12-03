# DadaBot
A Discord bot for sharing audio

## Getting DadaBot
If you don't care about the code, and just want to run DadaBot, click [here](https://github.com/Scrattlebeard/DadaBot/raw/master/release/DadaBot.zip) to download a .zip file containing everything you need.

You are of course also very welcome to clone this repo, build DadaBot yourself and even submit a pull request with a nice new feature.

## Configuring DadaBot
DadaBot expects a DadaBot.config.json file to be present in the same directory as the DadaBot.exe file. You will need to change the contents of this file to run DadaBot on your machine. Don't worry, this can be done in a simple text editor such as Notepad.

#### Creating a Discord Bot
You need to create a Discord Application which DadaBot can log in as. I also highly recommend that you enable Developer Mode in Discord, which will give you easy access to server, channel and user ids. [This](https://github.com/discord-apps/bot-tutorial) guide explains how to do both. Another guide, which also covers adding the bot to a server can be found [here](https://dsharpplus.github.io/articles/basics/bot_account.html).

#### Addding a bot to a server
You need to go to the OAuth2 management tab of your application to create an authorization url. You can then paste that url into your browser, to complete the process. Section 5 of [this](https://www.writebots.com/discord-bot-token/) guide explains the process in more detail. DadaBot needs the "Connect" and "Speak" permissions under "Voice". If you want to use the Greet command to make DadaBot introduce itself, it will also need the "Send Messages" permission.

#### DadaBot.config.json explanation
- DiscordSettings: Contains settings related to Discord
  - Token: This is the token from Discord Bot you created. It tells DadaBot to connect "as" that bot.
  - LogLevel: Valid values are "Trace", "Debug", "Info", "Warning", "Error" and "Fatal". Change this to "Debug" if you encounter issues and don't know what's wrong.
  - OwnerId: Add your DiscordId here to tell DadaBot who's the boss. You can get it by right-clicking on yourself after enabling Developer mode on Discord.
  - AutoJoin: This can be true or false. When true, DadaBot will automatically join your current channel when you give it a play command, if it isn't already in a channel. Leave it to false, if you want to manually tell DadaBot where to go all the time.
  
- SoundSettings: Contains settings related to Audio  
  - InputDeviceName: The name of the device you want to broadcast sound from.
  - InputDeviceId (optional): The device id of the sound device you want to broadcast sound from.
  - PlayLocally: If this is set to true, DadaBot will both output sound in a channel and on your default output device. Leave it false if you plan to actually be in the DadaBot channel.
  - SampleSize: The size of each audio sample sent to Discord. Valid values are 5, 10, 20, 40 and 60
  - BufferDurationMs: How many milliseconds DadaBot should buffer for before broadcasting. Max buffer size is 5000 ms, so you don't get any benefit from going higher than that.

##### Dadabot.config.json example
```json
{
    "DiscordSettings": {
        "Token": "your_token_here",
        "LogLevel": "Info",
        "OwnerId": 128524271700934657,
        "AutoJoin":  true
    },
    "SoundSettings": {
        "InputDeviceId": "{0.0.0.00000000}.{41515c9a-8763-4df6-adc0-ff6412e2519e}",
        "InputDeviceName": "Digital Audio (S/PDIF)",
        "PlayLocally":  false,
        "SampleSize": 40,
        "BufferDurationMs": 2000
    }
}
```

## Setting up Sound Capture
First you need to figure out what device you want to capture audio from. This should not be your default device, since that will result in DadaBot capturing sound from itself if you join the channel. I use my Digital Audio output device which isn't connected to anything. Such a device is present on most computers, in my experience.

The name of your sound device is what Windows shows you in the Volume Mixer and various other sound setting managers.

After updating the configuration file with the values, you need to set your desired application(s) to use the output. This can be done by right-clicking on the speaker icon in the task bar and selecting "Open Sound settings". From here, select "App volume and device preferences" under "Advanced sound options". From here, you can change the output of your running applications. Note: Changing the output means that you most likely won't hear anything on your end before DadaBot starts playing.

## Running DadaBot
Open a Powershell window or Command Prompt in the directory where you extracted the DadaBot.zip. Run DadaBot with the command ".\DadaBot.exe".

## DadaBot commands
You can Direct Message commands to DadaBot if you prefix them with an exclamation mark (!). You can also type directly in a channel on a server DadaBot is on, but I don't recommend that due to the spam. I recommend typing your commands directly in the command window where you run DadaBot.exe. There, you don't need to use the "!" prefix.

DadaBot currently supports the following commands:

##### Join (aliases: join, j)
Makes DadaBot join a channel. Examples:
- "join 690208034124523458" - Joins the channel with the given id
- "j" - Joins whatever channel you are in
- "join" - Same as above
- "join VoiceChannel" - Joins the channel named "VoiceChannel". Not recommended due to issues with channel names containing spaces. Also requires the channel name to be unique across all servers DadaBot is a part of.

#### Leave (aliases: leave, l, quit, q)
Makes DadaBot leave the channel it is currently in. Examples:
- "leave"
- "q"

#### Play (aliases: play, p)
Makes DadaBot start capturing and playing sound. If autojoin is enabled and DadaBot isn't already in a channel, it will join you automagically. Examples:
- "p"
- "play"

#### Stop (aliases: stop, s)
Makes DadaBot stop the playback. Examples:
- "stop"

#### Greet (aliases: greet)
Makes DadaBot post a brief message introducing itself. It needs the server and optionally the channel to post in.
- "greet 690208034124523458 301208034124523458" - DadaBot will post its greeting in server 69... and channel 30...
- "greet 690208034124523458" - DadaBot will post its greeting in the channel designated as "general" in serve 69...
- "greet ServerName ChannelName" - DadaBot will post its greeting in server "ServerName"'s channel "ChannelName". Not recommended for the same reason as in the join command.


Have fun with DadaBot!

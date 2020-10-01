When enabled, Life Support will prevent a player’s death and restore their health to 100%. Optionally you can charge Reward Points on three scales. Default, VIP and Admin. Life Support will not prevent death if a player does not have enough reward points.

This plugin is intended for PvE servers. The idea is to reward players for spending more time on your server, and to make is easier for solo player to defeat the hard monuments like Oil Rig.

## Features

* Prevents player from dying
* Rewards players for spending more time on your server
* Allow your admins and moderators to be invincible without using god mode. 
 
## Permissions

* `lifesupport.default`
* `lifesupport.vip`
* `lifesupport.admin`

Set the amount of Reward Points to charge (if any) in the configuration file.

## Chat Commands

* `/lifesupport` - Toggle Life Support on and off.

## Configuration

```json
{
     "Use Server Rewards (true/false)": false,
     "Exclude RaidableBases Zones (true/false)": true,
     "Enable Log file (true/false)": true,
     "Log output to console (true/false)": true,
     "Permissions and cost": {
     "lifesupport.default": 400,
     "lifesupport.vip": 200,
     "lifesupport.admin": 0
    }
}
```

If Use Server Rewards is set to false Life Support will be free to everyone who has any permission level. 

Set "Exclude RaidableBases Zones" to true to disable LifeSupport when in a RaidableBase zone.

Log file is written to oxide/logs/lifesupport and can be turned off by setting Enable Log file to false.

Log to console can be disabled by setting Log output to console to false.

Cost can be set to any value so you can adjust to how many reward points you give out. I have my server set to 50 reward points every 30 minutes, so it takes 4 hours of play time per life at default level. 

## Localization
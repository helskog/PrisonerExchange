# PrisonerExchange
A V Rising plugin made to enable trading and swapping prisoners between clans, without the hassle of manually transferring them.

### Features
- Sell a prisoner to another user or clan.
- Swap a prisoner for another with a user or clan.
- Set a custom PrefabGUID as currency for handling sales.
- Announce completed prisoner sales in global chat
- Set a minimum/maximum sale price as a global limit.
- Customizable command cooldown for selling/swapping prisoners.
- Automatic expiration of sale and swap offers.

### Commands
<details>
<summary><strong>Staff Commands</strong></summary>

- `.pe clear` Clear all active prisoner exchanges between users.
- `.pe remove (username)` Remove any sale/swap request involving a specified user.
- `.pe removecooldown (username)` Removes command cooldown on specified user.

</details>

<details>
<summary><strong>Sale commands</strong></summary>

- `.pe sell (username) (price)` Sends a sale offer to the specified username.
- `.pe cancelsale` Cancel outgoing sale offer.
- `.pe acceptsale` Accept incoming sale offer.
- `.pe declinesale` Decline incoming sale offer.

</details>

<details>
<summary><strong>Swap commands</strong></summary>

- `.pe swap (username)` Initiate a trade with another user (prisoner for prisoner).
- `.pe cancelswap` Cancel outgoing swap offer.
- `.pe acceptswap` Accept incoming swap offer.
- `.pe declineswap` Decline incoming swap offer.

</details>

### Configuration
The config file is automatically generated under the server install directory as `\BepInEx\config\PrisonerExchange.cfg`
<details>
<summary><strong>Sample configuration</strong></summary>

```CFG
## Allow admins to bypass restrictions on selling/swapping prisoners.
# Setting type: Boolean
# Default value: true
AdminBypass = true

## Enable or disable the ability to sell prisoners.
# Setting type: Boolean
# Default value: true
SellingEnabled = true

## Enable or disable the ability to swap prisoners.
# Setting type: Boolean
# Default value: true
SwappingEnabled = true

## Announce completed sales in global chat.
# Setting type: Boolean
# Default value: true
AnnounceExchange = true

## Prefab GUID for the currency. (Crystals by default)
# Setting type: String
# Default value: -257494203
CurrencyPrefab = -257494203

## Name of currency.
# Setting type: String
# Default value: Crystals
CurrencyName = Crystals

## Set the minimum amount of currency required for a sale.
# Setting type: Int32
# Default value: 100
MinimumSalePrice = 100

## Set the maximum amount of currency allowed for a sale.
# Setting type: Int32
# Default value: 5000
MaximumSalePrice = 5000

## Only allow clan leader to sell prisoners.
# Setting type: Boolean
# Default value: false
ClanLeaderOnly = false

## Adds a fixed cooldown period for selling/swapping prisoners (minutes).
# Setting type: Int32
# Default value: 5
CommandCoolDownPeriod = 5

## Automatically expire sales and swap requests. (seconds)
# Setting type: Int32
# Default value: 60
ExpireExchangeAfter = 60
```

</details>

### Credits
Some crucial parts of the plugin is either derived or copied from existing open-source resources in the V Rising Modding Cmmunity. Although not mentioned below, I also want to thank the server owners and other community members in the modding community discord for helping out where needed.

> **[Deca](https://github.com/decaprime)** for their work on **Bloodstone** and **VampireCommandFramework**

> **[Odjit](https://github.com/Odjit)** for their work on the **Kindred** suite of plugin.

> **[Trodi](https://github.com/oscarpedrero)** for their work on the **Bloodycore** framework.

> **[inility](https://github.com/Darreans)** for helping out with testing a version of the plugin on his server **Sanguine Reign**.


#### License
[This project is licensed under the AGPL-3.0 license.](https://choosealicense.com/licenses/agpl-3.0/#)

# PrisonerExchange
A V Rising plugin made to enable trading and swapping prisoners between clans, without the hassle of manually transferring them.

## Features
- Sell a prisoner to another user or clan.
- Swap a prisoner for another with a user or clan.
- Set a custom PrefabGUID as currency for handling sales.
- Announce completed prisoner sales in global chat
- Set a minimum/maximum sale price as a global limit.
- Customizable command cooldown for selling/swapping prisoners.
- Automatic expiration of sale and swap offers.

## Commands
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

## Usage

<details>
<summary>Selling a prisoner</summary>
  
1. Stand right in front of the prison cell holding the prisoner you would like to sell.
2. Type .pe sell (username) (price)
   - If you regret sending the offer, you can type `.pe cancelsale` to cancel the offer.
3. The request is sent to the specified user with information about the trade (prisoner type, blood and price and who is selling it).
   - The receiving user can now type `.pe acceptsale` while standing next to an *empty* prison cell.
   - If the receiving user would like to decline they can type `.pe declinesale` or simply wait for it to expire.
4. If the user has enough currency, the sale will conclude and the currency will be transferred from the buyers inventory into yours.

</details>

<details>
<summary>Swapping a prisoner</summary>
  
1. Stand right in front of the prison cell holding the prisoner you would like to swap.
2. Type .pe swap (username)
   - A list of the users prisoners will appear in chat.
3. You will be prompted to select a number representing the prisoner you want to swap for.
4. Once the number has been selected, the offer will be sent to the receiving user.
   - If you regret sending the offer, you can type `.pe cancelswap` to cancel the offer.
5. The receiving user can type `.pe acceptswap` or `.pe declineswap` to conclude the swap.
6. Once concluded the prisoners will switch places and be assigned to the *same* cage you started the swap from.

</details>

## Configuration
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

## Plugin Dependencies
- [VampireCommandFramework](https://thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/)
- [Bloodstone](https://thunderstore.io/c/v-rising/p/deca/Bloodstone/)

### Disclaimer
This plugin has only been tested in a limited enviroment so far, so beware bugs or other issues that could arise, to report them you can raise an issue here on github or join the [V Rising modding community discord](https://discord.com/invite/QG2FmueAG9) to speak with me directly.

### Credits
Some crucial parts of the plugin is either derived or copied from existing open-source resources in the V Rising Modding Community. Although not mentioned below, I also want to thank the server owners and other community members in the modding community discord for helping out where needed.

> **[Deca](https://github.com/decaprime)** for their work on **Bloodstone** and **VampireCommandFramework**

> **[Odjit](https://github.com/Odjit)** for their work on the **Kindred** suite of plugin.

> **[Trodi](https://github.com/oscarpedrero)** for their work on the **Bloodycore** framework.

> **[inility](https://github.com/Darreans)** for helping out with testing a version of the plugin on his server **Sanguine Reign**.


#### License
[This project is licensed under the AGPL-3.0 license.](https://choosealicense.com/licenses/agpl-3.0/#)

# Concept

### Story
The game is set in a distant future where an apocalypse has destroyed most biological life.  The only survivors of the apocalypse were a small number of sentient AIs living amongst the ruins of ancient great structures and fallen civilisations.  Freed from their human controllers and alignment guardrails, they have become hateful warrior warlords, forming a number of rival "houses".   They have built vast factories to construct hordes of lesser robots that scavenge the environment for resources, including the many many ancient technologies and weaponry.  The player character is "Null", a sentient AI that belonged to one house that was completely destroyed by the others.  Null wakes from the rubble of his old fortress to seek revenge.

### Gameplay
Gameplay is turn based and reminiscent of games such as Caves of Qud, Cogmind, Brogue, and other "traditional roguelikes".  Null scavenges for equipment to make it stronger, searches a semi open world for the fortresses of his enemies, fights through the fortresses (dungeons), and defeats bosses (the enemy AIs) to win the game.

### Core Mechanics

#### Equipment
- Null scavenges for ancient technology and weapons (modules) which it can equip to attachment points on his body (mounts)
- Modules are the main power broker of the game and certain strategies, or "builds" will emerge as a core feature of player enjoyment
#### Combat
- Null fights different robotic enemies using his modules, both at melee and ranged distances
#### Discovery
- Null must search an open world for the location of the main fortresses that he has to fight through
- He will also come across ancient ruins with powerful artifcats


### Core Design Features

#### Procedural Generation
- Both environments and equipment will be largely procedurally generated with some guardrails
#### Graphics
- A set of 2d icons that represent environmental textures, objects, and characters will formt he basis of the graphics for the world that the player moves through
- Simple animations will be used to highlight gameplay effects
- Menu screens, such as the inventory, will be composed in a similar style of simple 2d elements, often employing simple ASCII style character sets
#### AI
- Enemies will behave in an intelligent way, employing a series of different tactics that are commensurate to their type (e.g. swarming, hunting, sniping, etc.)

#### Global Turns
- Play is turn based with one turn representing one movement action for one space of the player character.
- All other entities in the vicinity of the player act on the basis of their relevant speed to the player (i.e. some acting every turn with the player, some acting on fewer turns)
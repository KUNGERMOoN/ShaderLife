![A GIF showcasing a Game Of Life simulation being edited while running](/Assets/Docs/Draw2.gif)

# ShaderLife
ShaderLife is a shader-powered cellular automata engine capable of running any [Life-Like](https://conwaylife.com/wiki/List_of_Life-like_rules) rule, including Conway's Game Of Life.
To maximize performance, ShaderLife uses a custom, lookup table-based algorithm (see [below](#the-algorithm))

# How fast is it?
On my Radeon HD 8950 a 9600x9600 simulation (92 160 000 cells in total) runs in stable 60 FPS
(see the [video](https://www.youtube.com/watch?v=Vzda0ISLrko))

# How do I use it?
## Installation
- Download and unpack the `ShaderLife-win64.zip` from the newest [release](https://github.com/KUNGERMOoN/ShaderLife/releases/latest)
- Run `ShaderLife\ShaderLife.exe`
- If you see the `"This device doesn't support compute shaders which are required by this application"` error, try using a different GPU or updating your graphics drivers.

Note: Mac and Linux are currently not supported

## Input
| Keys | Action |
| --- | --- |
| WASD/Arrow keys | Camera movement |
| Mouse Scroll, PgUp/PgDown, E/Q | Zoom in/out |
| Left mouse button | Draw Cells |
| Right mouse button | Erase Cells |

**Note:** Camera movement is disabled when UI captures the focus. To bring the focus back to the simulation, click anywhere on the board

## Options
### General
| Name       | Action                                                                     |
| ---------- | -------------------------------------------------------------------------- |
| New Simulation (`+` sign) | Opens the ["New Simulation"](#new-simulation-popup) Popup |
| Play/Pause | Control whether the simulation should run in real time. Paused by default. |
| Next       | Manually triggers the next tick of the simulation.                         |
| Clear      | Resets the state of the simulation.                                        |
| Simulation Rate | The requested simulation speed, in ticks per second. When set to 0, the simulation will run every frame. **Note:** The actual simulation speed will be capped to the FPS. Its value is displayed in the top-right corner, as `"X ticks/sec"` |
| Seed       | Seed value used when randomizing the simulation. Automatically set to a random value after randomizing the board |
| Chance     | Chance for any cell to be alive when randomizing the board. **Note:** Selecting a too low or too high value will cause most cells to die after a single tick due to underpopulation or overpopulation |
| Randomize  | Randomizes the board, creating a ["Soup"](https://conwaylife.com/wiki/Soup) with the Seed and Chance (Density) specified above. |

### New Simulation popup
| Name       | Action                                                                     |
| ---------- | -------------------------------------------------------------------------- |
| Size | Controls the size of the new simulation. The actual size in cells is displayed below |
| Advanced/Lookup Table Path | Path to the `.lut` file containing the pre-computed data describing the [Life-Like](https://conwaylife.com/wiki/List_of_Life-like_rules) rule to be used in the next simulation. Press the "load" button to open the File Explorer |
| Advanced/New Lookup Table (`+` sign) | Opens the "New Lookup Table" Popup |

# The algorithm
This program uses a lookup table-based approach, inspired by some of the answers I found [here](https://stackoverflow.com/questions/40485/optimizing-conways-game-of-life)

The simulation space (or "board") is split into rectangular chunks, each 4 cells wide and 2 cells tall. Because every chunk contains exactly 8 cells, any "state" (or "configuration") of a chunk can be represented as 8 booleans, or a single byte (number).
Since there's a limited amount of possible chunk states, and simulating a chunk with the same state will always yield the same results, we can create a lookup table which will function as a sort of "cheat sheet" for our simulation. As input, it takes the state of the current chunk, and it's neighboring cells and returns the future state of that chunk as output. Since both of these are numerical values, we can implement the lookup table as a byte array, where the input (current state) is the index of the element in the array and the output (next state) is the data at that index. (eg. `byte newState = LookupTable[oldState]`)

As a result, when the simulation runs, each thread:
- gets the value of it's the corresponding chunk
- combines it with the values of the neighboring cells from the chunks around it
- uses that as an input index in the lookup table
- saves the data at that index in the lookup table as the new state of the chunk

This approach speeds up the simulation by (estimated) ~4 times, and allows for simulating algorithms other than Conway's Game Of Life without any additional computations at runtime (simply use a different lookup table).

# Credits
- UI icons from "[Google Material Symbols](https://fonts.google.com/icons?icon.set=Material+Symbols&icon.style=Rounded&icon.query=close)"
- Rasterization algorithm for drawing based on [this](https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm#All_cases) Wikipedia article
- Color palettes from Lospec: [Cold Light](https://lospec.com/palette-list/cold-light), [Titanstone](https://lospec.com/palette-list/titanstone), [Chocolate Ichor](https://lospec.com/palette-list/chocolate-ichor)

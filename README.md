# -not-SEGA-BIRTH
A 16-bit fantasy console ecosystem based on the Sega Genesis. Featuring a simulated and customized 68010 CPU (1.3x boost), LPN physics, and the proprietary .BGF binary format. Built for madmen who think 32KB of RAM is plenty

# (not)SEGA BIRTH Technical Specifications

(not)SEGA BIRTH is an open-source fantasy console designed to enforce strict hardware constraints. It operates as a virtualized environment where software performance is derived from architectural limitations rather than raw processing power.

## 1. Hardware Specifications

| Component | Specification | Description | Notes |
| --- | --- | --- | --- |
| **CPU** | Motorola 68010 (Simulated) | Features a 1.3x "Loop Mode" boost for high-frequency task execution. | It doesn't use the Motorola 68010 asm language look at `Opcode Reference` table|
| **Work RAM** | 32 KB | Maximum system memory; exceeding this limit results in a system crash. | The Zilog 120 (last entry on this table) doesn't get used for anything right now in the only example |
| **Video RAM** | 64 KB | Dedicated to 4-bit Birth Tile Protocol (BTP) assets. | BTP: Instead of storing full RGBA values for every pixel, it stores bit-indices. If you use a 16-color palette, each pixel only takes up 4 bits ($2^4 = 16$)|
| **Resolution** | 380 x 240 pixels | Widescreen format optimized for retro-style pixel art. | The screen size is doubled for visabilty in the .exe but not in the .cs file |
| **Math** | LPN (Levitating Point) | 16.16 fixed-point logic for sub-pixel precision without an FPU. | LPN is this console's version of FPU look at the Register Map |
| **Audio** | Zilog 120 (Simulated) | Supports raw PCM playback for sound effects and startup jingles. | The Z120 (Zilog 120) is not a real processor it's self-made |

## 2. System Architecture

The console ecosystem is divided into three distinct functional layers:

* **The Silicon (`Program.cs`)**: A C# virtual machine that enforces the 32KB RAM cap and manages the rendering pipeline via Raylib-cs.
* **The Factory (`builder.py`)**: A Python-based utility that assembles `.asm` source files and converts PNG images into 4-bit BTP tiles.
* **The Cartridge (`.BGF`)**: A unified binary format featuring the `BIRTH_EXEC` magic header for boot verification.

## 3. Instruction Set Architecture (ISA)

The ISA utilizes a 16-bit linear execution model. Instructions are executed sequentially unless modified by jump commands.

### Register Map

* **RegA**: 8-bit accumulator used for small values and X-axis screen coordinates.
* **RegX**: 16-bit general-purpose register used for indexing, Y-axis coordinates, and LPN arithmetic.

### Opcode Reference

| Opcode | Mnemonic | Argument | Description |
| --- | --- | --- | --- |
| **0x01** | `SET_A` | [val] | Load 8-bit value into RegA. |
| **0x02** | `SET_X` | [val] | Load 16-bit value into RegX. |
| **0x04** | `JMP` | [addr] | Update Program Counter to target address. |
| **0x08** | `DRAW_BTP` | None | Render VRAM tile at (RegA, RegX) coordinates. |
| **0xFF** | `HALT` | None | Terminate CPU cycles for the current frame. |

## 4. Development Workflow

### Build Process

Compile assembly and graphics into a bootable cartridge:
`python builder.py game.asm hero.png` or use the `BuildCart.bat` file

### Console Execution

Initialize the virtual machine and load the `.BGF` file:
`dotnet run` 

## 5. Credits and Dependencies

* **Raylib-cs**: Manages rendering, audio, and input cross-platform.
* **Pillow (PIL)**: Executes PNG to BTP tile conversion and color dithering.
* **Motorola 68010**: The architectural inspiration for the simulated processing core.

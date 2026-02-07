using Raylib_cs;
using System.Runtime.InteropServices;
using System.Text;

class NotSegaBirth {
    // --- HARDWARE SPECS ---
    public const int ScreenW = 380;
    public const int ScreenH = 240;

    // Memory
    public byte[] RAM = new byte[32768];    // 32KB Code/Work RAM
    public byte[] VRAM = new byte[65536];   // 64KB Graphics RAM
    
    // Registers
    public byte RegA = 0;      // 8-bit Accumulator
    public ushort RegX = 0;    // 16-bit Index Register (Fixes Overflow!)
    public int PC = 0;         // Program Counter
    
    // Internal State
    public int CodeSize = 0;
    public int GfxStartAddr = 0;

    public void Boot(string cartPath) {
        if (!File.Exists(cartPath)) {
            Console.WriteLine("❌ ERROR: Cartridge not found.");
            return;
        }

        using (BinaryReader reader = new BinaryReader(File.Open(cartPath, FileMode.Open))) {
            // 1. HEADER CHECK
            byte[] magic = reader.ReadBytes(10);
            string magicStr = Encoding.ASCII.GetString(magic);
            
            if (magicStr != "BIRTH_EXEC") {
                Console.WriteLine($"❌ INVALID FORMAT: {magicStr}");
                return;
            }

            int entryPoint = reader.ReadInt32(); // Unused for now
            CodeSize = reader.ReadInt32();
            int gfxSize = reader.ReadInt32();

            Console.WriteLine($"✅ CARTRIDGE LOADED");
            Console.WriteLine($"   Code: {CodeSize} bytes");
            Console.WriteLine($"   GFX:  {gfxSize} bytes");

            // 2. LOAD CODE INTO RAM (At offset 0)
            byte[] code = reader.ReadBytes(CodeSize);
            Array.Copy(code, RAM, code.Length);

            // 3. LOAD GRAPHICS INTO VRAM
            byte[] gfx = reader.ReadBytes(gfxSize);
            Array.Copy(gfx, VRAM, Math.Min(gfx.Length, VRAM.Length));
        }

        Run();
    }

    public void Run() {
        Raylib.InitWindow(ScreenW * 2, ScreenH * 2, "(not)SEGA BIRTH - DevKit v1.0");
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose()) {
            // --- CPU CYCLE ---
            // We run multiple opcodes per frame to simulate speed
            int cycles = 0;
            while (cycles < 10 && PC < CodeSize) { 
                ExecuteOpcode();
                cycles++;
            }

            // --- RENDER CYCLE ---
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            // Debug Info
            Raylib.DrawText($"PC: {PC}", 10, 10, 20, Color.Green);
            Raylib.DrawText($"RegA: {RegA}", 10, 30, 20, Color.Green);
            Raylib.DrawText($"RegX: {RegX}", 10, 50, 20, Color.Green);
            
            // Note: Actual drawing happens in the Opcode for now, 
            // but normally you'd render VRAM here.
            
            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }

    void ExecuteOpcode() {
        byte op = RAM[PC];
        PC++; // Advance past opcode

        switch (op) {
            case 0x00: // NOP
                break; 

            case 0x01: // SET_A (1 Byte Arg)
                RegA = RAM[PC];
                PC += 1;
                break;

            case 0x02: // SET_X (2 Byte Arg)
                RegX = BitConverter.ToUInt16(RAM, PC);
                PC += 2;
                break;

            case 0x04: // JMP (2 Byte Arg)
                ushort target = BitConverter.ToUInt16(RAM, PC);
                // Safety check to prevent jumping out of bounds
                if (target < CodeSize) {
                    PC = target;
                } else {
                    Console.WriteLine("❌ SEGFAULT: JUMP OUT OF BOUNDS");
                    PC = CodeSize; // Halt
                }
                break;

            case 0x08: // DRAW_BTP (4 Byte Arg: X, Y)
                // Draw 8x8 Tiles from VRAM (Simplified logic)
                short x = BitConverter.ToInt16(RAM, PC);
                short y = BitConverter.ToInt16(RAM, PC + 2);
                PC += 4;
                
                // Hacky Render for now:
                // We assume VRAM contains valid BTP data at 0
                DrawBTPFromVRAM(x, y);
                break;

            case 0xFF: // HALT
                PC = CodeSize; 
                break;
        }
    }
    
    // Reads raw tile data from VRAM and draws it
    void DrawBTPFromVRAM(int screenX, int screenY) {
        // Read BTP Header from VRAM
        if (VRAM.Length < 4) return;
        
        ushort w = BitConverter.ToUInt16(VRAM, 0);
        ushort h = BitConverter.ToUInt16(VRAM, 2);
        
        int ptr = 4; // Start of pixels in VRAM
        
        for (int ty = 0; ty < h; ty += 8) {
            for (int tx = 0; tx < w; tx += 8) {
                // For each 8x8 block
                for(int py = 0; py < 8; py++) {
                    for(int px = 0; px < 8; px+=2) {
                        if (ptr >= VRAM.Length) return;
                        byte packed = VRAM[ptr++];
                        
                        // Draw Pixel 1
                        Color c1 = (packed >> 4) > 0 ? Color.White : Color.Black; 
                        // Draw Pixel 2
                        Color c2 = (packed & 0x0F) > 0 ? Color.White : Color.Black;

                        // Draw to Raylib (Scale 2x)
                        Raylib.DrawRectangle((screenX + tx + px) * 2, (screenY + ty + py) * 2, 2, 2, c1);
                        Raylib.DrawRectangle((screenX + tx + px + 1) * 2, (screenY + ty + py) * 2, 2, 2, c2);
                    }
                }
            }
        }
    }
}

class Program {
    static void Main(string[] args) {
        new NotSegaBirth().Boot("game.BGF");
    }
}
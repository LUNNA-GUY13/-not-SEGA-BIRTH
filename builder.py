import sys
import struct
import os
from PIL import Image

# --- CONFIGURATION ---
OPCODES = {
    "NOP": 0x00, 
    "SET_A": 0x01,    # 8-bit value
    "SET_X": 0x02,    # 16-bit value (0-65535)
    "PLOT": 0x03,     # Uses RegA (Color) and RegX (Position)
    "JMP": 0x04,      # 16-bit Address
    "VSRAM": 0x05,    # 16-bit Offset
    "DRAW_BTP": 0x08, # 16-bit X, 16-bit Y
    "HALT": 0xFF
}

def convert_graphics(img_path):
    if not os.path.exists(img_path):
        print(f"⚠️ Warning: {img_path} not found. Cartridge will have no graphics.")
        return b""
        
    print(f"🎨 Processing Image: {img_path}...")
    try:
        img = Image.open(img_path).convert('P', palette=Image.ADAPTIVE, colors=16)
    except Exception as e:
        print(f"❌ PIL Error: {e}")
        return b""

    width, height = img.size
    tile_data = bytearray()
    
    # 4-Byte Header for Graphics Block (Width, Height)
    tile_data.extend(struct.pack("<HH", width, height)) 
    
    # Encode 8x8 Tiles
    for ty in range(0, height, 8):
        for tx in range(0, width, 8):
            for y in range(8):
                for x in range(0, 8, 2):
                    if tx+x+1 >= width or ty+y >= height: continue
                    p1 = img.getpixel((tx + x, ty + y)) & 0x0F
                    p2 = img.getpixel((tx + x + 1, ty + y)) & 0x0F
                    tile_data.append((p1 << 4) | p2)
    return tile_data

def assemble_cart(asm_path, output_path, btp_data=b""):
    print(f"🔨 Assembling Code: {asm_path}...")
    
    raw_instructions = []
    labels = {} # Stores label_name -> byte_index
    current_byte_index = 0

    with open(asm_path, 'r') as f:
        lines = f.readlines()

    # --- PASS 1: PARSE & RECORD LABELS ---
    clean_lines = []
    for line in lines:
        line = line.split(';')[0].strip() # Remove comments
        if not line: continue
        
        # Check for Label (e.g., "@Start:")
        if line.startswith("@"):
            label_name = line.replace(":", "")
            labels[label_name] = current_byte_index
            continue
            
        clean_lines.append(line)
        
        # Calculate size for Pass 2
        parts = line.replace(',', '').split()
        cmd = parts[0].upper()
        if cmd in OPCODES:
            current_byte_index += 1 # Opcode is 1 byte
            # Add argument sizes
            if cmd == "SET_A": current_byte_index += 1
            elif cmd in ["SET_X", "JMP", "VSRAM"]: current_byte_index += 2
            elif cmd == "DRAW_BTP": current_byte_index += 4

    # --- PASS 2: GENERATE BYTECODE ---
    code_block = bytearray()
    
    for line in clean_lines:
        parts = line.replace(',', '').split()
        cmd = parts[0].upper()
        
        if cmd in OPCODES:
            code_block.append(OPCODES[cmd])
            
            if cmd == "SET_A":
                val = int(parts[1])
                code_block.append(val & 0xFF) # Force 8-bit
                
            elif cmd in ["SET_X", "VSRAM"]:
                val = int(parts[1])
                code_block.extend(struct.pack("<H", val)) # 16-bit Little Endian
                
            elif cmd == "JMP":
                target = parts[1]
                if target in labels:
                    addr = labels[target]
                else:
                    addr = int(target)
                code_block.extend(struct.pack("<H", addr))
                
            elif cmd == "DRAW_BTP":
                x = int(parts[1])
                y = int(parts[2])
                code_block.extend(struct.pack("<HH", x, y))

    # --- FINAL BUILD: THE BGF CONTAINER ---
    final_cart = bytearray()
    
    # 1. Header
    final_cart.extend(b"BIRTH_EXEC") # Magic Signature (10 bytes)
    final_cart.extend(struct.pack("<I", 0)) # Entry Point (Placeholder 0)
    final_cart.extend(struct.pack("<I", len(code_block))) # Code Size
    final_cart.extend(struct.pack("<I", len(btp_data)))   # Gfx Size
    
    # 2. Sections
    final_cart.extend(code_block)
    final_cart.extend(btp_data)

    with open(output_path, 'wb') as f:
        f.write(final_cart)
        
    print(f"✅ BGF Generated: {output_path}")
    print(f"   - Code Size: {len(code_block)} bytes")
    print(f"   - GFX Size:  {len(btp_data)} bytes")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python builder.py <game.asm> [graphics.png]")
    else:
        gfx = b""
        if len(sys.argv) > 2:
            gfx = convert_graphics(sys.argv[2])
        assemble_cart(sys.argv[1], "game.BGF", gfx)
namespace Chip8WpfApp.Interpreter
{
    internal class Chip8
    {
        // http://devernay.free.fr/hacks/chip8/C8TECH10.HTM#8xy3
        // https://github.com/ronazulay/Chip8/blob/master/Chip8/Vm/Vm.cs
        // https://github.com/DanTup/DaChip8/blob/d6bd0edefcd4e463069e8f8f91b740c40d3f1ffe/DaChip8/Chip8.cs#L141
        // https://multigesture.net/articles/how-to-write-an-emulator-chip-8-interpreter/
        // https://en.wikipedia.org/wiki/CHIP-8#Opcode_table
        // https://github.com/JamesGriffin/CHIP-8-Emulator/blob/master/src/chip8.cpp

        // Example:
        // number 4 - 0x90, 0x90, 0xF0, 0x10, 0x10
        // 0x90 - 1001 - *  *
        // 0x90 - 1001 - *  *
        // 0xF0 - 1111 - ****
        // 0x10 - 0001 -    *
        // 0x10 - 0001 -    *
        private byte[] fonts = new byte[]
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        };

        private ushort opCode = 0;

        // Program counter
        private ushort pc = 0x200;

        // Stack position
        private ushort sp = 0;
        private ushort[] stack = new ushort[0xC];

        private byte delayTimer = 0;
        private byte soundTimer = 0;

        // Registers
        private byte[] v = new byte[0x10];

        // 16-bit register (for memory address)
        private ushort i = 0;
        private byte[] memory = new byte[0x1000];

        public byte[] graphics = new byte[0x800]; // Screen: 0x40 * 0x20
        public bool drawFrame = true;

        private bool[] keys = new bool[0x10];

        public void ResetIntepreter()
        {
            pc = 0x200;
            sp = 0;
            i = 0;
            graphics = new byte[0x800];
        }

        public void LoadROM(byte[] rom)
        {
            Array.Copy(rom, 0, memory, pc, rom.Length);
        }

        public void LoadFonts()
        {
            Array.Copy(fonts, 0, memory, 0, fonts.Length);
        }

        public void KeyUp(byte key)
        {
            keys[key] = false;
        }

        public void KeyDown(byte key)
        {
            keys[key] = true;
        }

        public void EmulateCycle()
        {
            // Example: Memory[PC] = 0xA2 (1010 0010)
            // 0xA2 << 8 = 1010 0010 0000 0000
            // Memory[PC + 1] = 0xF0 (1111 0000)
            // 1010 0010 0000 0000 | 1111 0000 = 1010 0010 1111 0000 (0xA2F0)
            opCode = (ushort)(memory[pc] << 8 | memory[pc + 1]);
            pc += 2;

            drawFrame = false;

            // Example: 0xA2F0 & 0x0FFF = 0x2F0
            ushort nnn = (ushort)(opCode & 0x0FFF); // Address

            // Example: 0xA2F0 & 0x00FF = 0xF0 (1111 0000)
            byte nn = (byte)(opCode & 0x00FF); // 8-bit constant

            // Example: 0xA2F0 & 0x000F = 0000
            byte n = (byte)(opCode & 0x000F); // 4-bit constant

            // Example: 0xA2F0 & 0x0F00 = 0x200 >> 8 = 0x2 (0010)
            byte x = (byte)((opCode & 0x0F00) >> 8); // 4-bit register identifier

            // Example: 0xA2F0 & 0x00F0 = 0xF0 >> 4 = 0xF (1111)
            byte y = (byte)((opCode & 0x00F0) >> 4); // 4-bit register identifier

            switch (opCode & 0xF000)
            {
                case 0x0000 when opCode == 0x00E0:
                    OpCode00E0();
                    break;
                case 0x0000 when opCode == 0x00EE:
                    OpCode00EE();
                    break;
                case 0x0000:
                    OpCode0NNN(nnn);
                    break;
                case 0x1000:
                    OpCode1NNN(nnn);
                    break;
                case 0x2000:
                    OpCode2NNN(nnn);
                    break;
                case 0x3000:
                    OpCode3XNN(x, nn);
                    break;
                case 0x4000:
                    OpCode4XNN(x, nn);
                    break;
                case 0x5000:
                    OpCode5XY0(x, y);
                    break;
                case 0x6000:
                    OpCode6XNN(x, nn);
                    break;
                case 0x7000:
                    OpCode7XNN(x, nn);
                    break;
                case 0x8000 when (opCode & 0x000F) == 0:
                    OpCode8XY0(x, y);
                    break;
                case 0x8000 when (opCode & 0x000F) == 0x1:
                    OpCode8XY1(x, y);
                    break;
                case 0x8000 when (opCode & 0x000F) == 0x2:
                    OpCode8XY2(x, y);
                    break;
                case 0x8000 when (opCode & 0x000F) == 0x3:
                    OpCode8XY3(x, y);
                    break;
                case 0x8000 when (opCode & 0x000F) == 0x4:
                    OpCode8XY4(x, y);
                    break;
                case 0x8000 when (opCode & 0x000F) == 0x5:
                    OpCode8XY5(x, y);
                    break;
                case 0x8000 when (opCode & 0x000F) == 0x6:
                    OpCode8XY6(x, y);
                    break;
                case 0x8000 when (opCode & 0x000F) == 0x7:
                    OpCode8XY7(x, y);
                    break;
                case 0x8000 when (opCode & 0x000F) == 0xE:
                    OpCode8XYE(x);
                    break;
                case 0x9000:
                    OpCode9XY0(x, y);
                    break;
                case 0xA000:
                    OpCodeANNN(nnn);
                    break;
                case 0xB000:
                    OpCodeBNNN(nnn);
                    break;
                case 0xC000:
                    OpCodeCXNN(x, nn);
                    break;
                case 0xD000:
                    OpCodeDXYN(x, y, n);
                    break;
                case 0xE000 when (opCode & 0x00FF) == 0x9E:
                    OpCodeEX9E(x);
                    break;
                case 0xE000 when (opCode & 0x00FF) == 0xA1:
                    OpCodeEXA1(x);
                    break;
                case 0xF000 when (opCode & 0x00FF) == 0x07:
                    OpCodeFX07(x);
                    break;
                case 0xF000 when (opCode & 0x00FF) == 0x0A:
                    OpCodeFX0A(x);
                    break;
                case 0xF000 when (opCode & 0x00FF) == 0x15:
                    OpCodeFX15(x);
                    break;
                case 0xF000 when (opCode & 0x00FF) == 0x18:
                    OpCodeFX18(x);
                    break;
                case 0xF000 when (opCode & 0x00FF) == 0x1E:
                    OpCodeFX1E(x);
                    break;
                case 0xF000 when (opCode & 0x00FF) == 0x29:
                    OpCodeFX29(x);
                    break;
                case 0xF000 when (opCode & 0x00FF) == 0x33:
                    OpCodeFX33(x);
                    break;
                case 0xF000 when (opCode & 0x00FF) == 0x55:
                    OpCodeFX55(x);
                    break;
                case 0xF000 when (opCode & 0x00FF) == 0x65:
                    OpCodeFX65(x);
                    break;
                default:
                    throw new InvalidOperationException($"Error: OpCode missing {opCode}");
            }
        }

        public void UpdateTimers()
        {
            if (delayTimer > 0)
                delayTimer--;

            if (soundTimer > 0)
            {
                Console.Beep(440, 100);
                soundTimer--;
            }
        }

        // Calls machine code routine (RCA 1802 for COSMAC VIP) at address NNN.
        // Not necessary for most ROMs
        private void OpCode0NNN(ushort nnn)
        {
            throw new NotImplementedException($"Error: {opCode} has not been implemented");
        }

        // Clears the screen
        private void OpCode00E0()
        {
            graphics = new byte[0x800];
        }

        // Returns from a subroutine
        private void OpCode00EE()
        {
            pc = stack[sp--];
        }

        // Jumps to address NNN
        private void OpCode1NNN(ushort nnn)
        {
            pc = nnn;
        }

        // Calls subroutine at NNN
        private void OpCode2NNN(ushort nnn)
        {
            stack[++sp] = pc;
            pc = nnn;
        }

        // Skips the next inctruction if V[X] == NN
        private void OpCode3XNN(byte x, byte nn)
        {
            if (v[x] == nn)
                pc += 2;
        }

        // Skips the next instruction if V[X] != NN
        private void OpCode4XNN(byte x, byte nn)
        {
            if (v[x] != nn)
                pc += 2;
        }

        // Skip the next instruction if V[X] == V[Y]
        private void OpCode5XY0(byte x, byte y)
        {
            if (v[x] == v[y])
                pc += 2;
        }

        // Sets V[X] == NN
        private void OpCode6XNN(byte x, byte nn)
        {
            v[x] = nn;
        }

        // Adds NN to V[X]
        private void OpCode7XNN(byte x, byte nn)
        {
            v[x] += nn;
        }

        // Sets V[X] = V[Y]
        private void OpCode8XY0(byte x, byte y)
        {
            v[x] = v[y];
        }

        // Sets V[X] = V[X] | V[Y]
        private void OpCode8XY1(byte x, byte y)
        {
            v[x] |= v[y];
        }

        // Sets V[X] = V[X] & V[Y]
        private void OpCode8XY2(byte x, byte y)
        {
            v[x] &= v[y];
        }

        // Sets V[X] = V[X] ^ V[Y]
        private void OpCode8XY3(byte x, byte y)
        {
            v[x] ^= v[y];
        }

        // The values of Vx and Vy are added together.
        // If the result is greater than 8 bits (i.e., > 255,) VF is set to 1, otherwise 0
        private void OpCode8XY4(byte x, byte y)
        {
            v[0xF] = (byte)((v[x] + v[y]) > 0xFF ? 1 : 0);

            v[x] += v[y];
        }

        // If Vx > Vy, then VF is set to 1, otherwise 0.
        // Then Vy is subtracted from Vx, and the results stored in Vx
        private void OpCode8XY5(byte x, byte y)
        {
            v[0xF] = (byte)(v[x] > v[y] ? 1 : 0);

            v[x] -= v[y];
        }

        // If the least-significant bit of Vx is 1, then VF is set to 1, otherwise 0.
        // Then Vx is divided by 2
        private void OpCode8XY6(byte x, byte y)
        {
            v[0xF] = (byte)((v[x] & 0x1) != 0 ? 1 : 0);

            v[x] >>= 0x1;
        }

        // If Vy > Vx, then VF is set to 1, otherwise 0.
        // Then Vx is subtracted from Vy, and the results stored in Vx
        private void OpCode8XY7(byte x, byte y)
        {
            v[0xF] = (byte)(v[y] > v[x] ? 1 : 0);

            v[x] = (byte)(v[y] - v[x]);
        }

        // If the most-significant bit of Vx is 1, then VF is set to 1, otherwise to 0.
        // Then Vx is multiplied by 2
        private void OpCode8XYE(byte x)
        {
            v[0xF] = (byte)(v[x] >> 7);

            v[x] <<= 0x1;
        }

        // The values of Vx and Vy are compared, and if they are not equal,
        // the program counter is increased by 2
        private void OpCode9XY0(byte x, byte y)
        {
            if (v[x] != v[y])
                pc += 2;
        }

        // The value of register I is set to NNN
        private void OpCodeANNN(ushort nnn)
        {
            i = nnn;
        }

        // The program counter is set to nnn plus the value of V0
        private void OpCodeBNNN(ushort nnn)
        {
            pc = (ushort)(nnn + v[0]);
        }

        // The interpreter generates a random number from 0 to 255,
        // which is then ANDed with the value NN. The results are stored in V[X]
        private void OpCodeCXNN(byte x, byte nn)
        {
            Random random = new Random();

            v[x] = (byte)(random.Next(0, 0xFF) & nn);
        }

        // Draw
        private void OpCodeDXYN(byte x, byte y, byte n)
        {
            // Initialize the collision detection as no collision detected (yet)
            v[0xF] = 0;
            
            // Draw N lines on the screen
            for (int yLine = 0; yLine < n; yLine++)
            {
                // Fetch the pixel value from the memory starting at location I
                byte pixel = memory[i + yLine];

                // Loop over 8 bits of one row
                for (int xLine = 0; xLine < 8; xLine++)
                {
                    // Check if the current evaluated pixel is set to 1
                    // (note that scan through the byte, one bit at the time)
                    if ((pixel & (0x80 >> xLine)) != 0)
                    {
                        // Check if the pixel on the display is set to 1.
                        // If it is set, we need to register the collision by setting the VF register
                        if (graphics[((v[x] + xLine) % 64) + ((v[y] + yLine) % 32 * 64)] == 1)
                            v[0xF] = 1;

                        // Set the pixel value by using XOR
                        graphics[((v[x] + xLine) % 64) + ((v[y] + yLine) % 32 * 64)] ^= 1;
                    }
                }
            }
            drawFrame = true;
        }

        // Checks the keyboard, and if the key corresponding to the value of Vx is currently in the down position,
        // PC is increased by 2
        private void OpCodeEX9E(byte x)
        {
            if (keys[v[x]])
                pc += 2;
        }

        // Checks the keyboard, and if the key corresponding to the value of Vx is currently in the up position,
        // PC is increased by 2
        private void OpCodeEXA1(byte x)
        {
            if (!keys[v[x]])
                pc += 2;
        }

        // Sets VX to the value of the delay timer
        private void OpCodeFX07(byte x)
        {
            v[x] = delayTimer;
        }

        // Wait for a key press, store the value of the key in Vx.
        // All execution stops until a key is pressed, then the value of that key is stored in Vx.
        private void OpCodeFX0A(byte x)
        {
            bool keyPressed = false;

            for (byte i = 0; i < 0x10; i++)
            {
                if (keys[i] != false)
                {
                    v[x] = i;
                    keyPressed = true;
                }
            }

            if (!keyPressed)
                pc -= 2;
        }

        // Sets the delay timer to VX
        private void OpCodeFX15(byte x)
        {
            delayTimer = v[x];
        }

        // Sets the sound timer to VX
        private void OpCodeFX18(byte x)
        {
            soundTimer = v[x];
        }

        // The values of I and Vx are added, and the results are stored in I
        private void OpCodeFX1E(byte x)
        {
            i += v[x];
        }

        // The value of I is set to the location for the hexadecimal sprite corresponding to the value of Vx
        private void OpCodeFX29(byte x)
        {
            i = (ushort)(v[x] * 0x5);
        }

        // Stores the binary-coded decimal representation of VX, with the hundreds digit in memory at location in I,
        // the tens digit at location I+1, and the ones digit at location I+2
        private void OpCodeFX33(byte x)
        {
            memory[i] = (byte)(v[x] / 0x64);
            memory[i + 1] = (byte)(v[x] / 0xA % 0xA);
            memory[i + 2] = (byte)(v[x] % 0xA);
        }

        // The interpreter copies the values of registers V0 through Vx into memory, starting at the address in I
        private void OpCodeFX55(byte x)
        {
            for (byte b = 0; b <= x; b++)
                memory[i + b] = v[b];
        }

        // Fills from V0 to VX (including VX) with values from memory, starting at address I.
        // The offset from I is increased by 1 for each value read, but I itself is left unmodified
        private void OpCodeFX65(byte x)
        {
            for (byte b = 0; b <= x; b++)
                v[b] = memory[i + b];
        }
    }
}
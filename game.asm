; --- INIT ---
SET_A 255
SET_X 20      ; We can now set X to whatever (16-bit!)

@LOOP:
  DRAW_BTP 100 100  ; Draw sprite at 100, 100
  DRAW_BTP 200 100  ; Draw another at 200, 100
  JMP @LOOP         ; Jump back to start!
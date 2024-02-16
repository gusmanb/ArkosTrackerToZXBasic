using ArkosTrackerToZXBasic;
using CommandLine;
using System.Diagnostics;
using System.Text;

const string songBuildTemplate = @"    include ""{0}.asm"" 
    include ""{0}_playerconfig.asm""
";

const string buildTemplate = @";Compiles the player, the music and sfxs, using RASM.
;No ORG needed.    

    {0}

    ;What hardware? Uncomment the right one.    
    PLY_{1}_HARDWARE_SPECTRUM = 1

    include ""Player{2}.asm""";

const string basicTemplate = @"'Address of out interrupt vector table
Dim intVectorTable as uInteger at $FE00
'Address of the JMP instruction. Its address' low and high byte MUST be the same
Dim intJmp as uInteger at $FDFD
'Use FRAME var, but ignore upper byte
Dim frameCounter as uInteger at $5C78

'Install the interrupt handler
sub fastcall InstallInterruptHandler()

asm

    di

    ld hl, _intVectorTable
    ld a, _intJmp
    ld i, a
    ld b, 128
    
INSTALL_TABLE:              ;generate a 257 bytes table pointing to the JMP instruction

    ld (hl), a
    inc hl
    ld (hl), a
    inc hl
    djnz INSTALL_TABLE
    ld (hl), a
    
    ld hl, _intJmp          ;generate a JMP to the interrupt handler
    ld de, INT_HANDLER
    ld a, $C3
    ld (hl), a
    inc hl
    ld (hl), e
    inc hl
    ld (hl), d

    IM 2
    ei
    
    ret

INT_HANDLER:                    ;main interrupt handler


    push af                     ;store all registers
    push hl

    ld hl, (_frameCounter)      ;increment the frame counter
    inc hl
    ld (_frameCounter), hl

    ld a, h
    or l
    jr nz, TEST_MUSIC
    ld a, (_frameCounter + 2)
    INC a
    ld (_frameCounter + 2), a

TEST_MUSIC:                     ;test if music is enabled

    ld a, (_enableMusic)
    and a
    jp z, INT_NO_MUSIC
    
    push bc                     ;preserve all registers
    push de
    push ix
    push iy

    exx
    ex af, af'

    push af
    push bc
    push de
    push hl

    call PLY_{0}_PLAY           ;play music

    pop hl                      ;restore all registers
    pop de
    pop bc
    pop af

    ex af, af'
    exx

    pop iy
    pop ix
    
    pop de
    pop bc

INT_NO_MUSIC:

    pop hl
    pop af

    ei                          ;reenable interrupts

    ret                         ;return from interrupt handler

end asm

end sub

sub PlayerHolder()

    asm

    jp SKIP_MUSIC

     _enableMusic: db 0

    {1}   

    SKIP_MUSIC:

    end asm

end sub

PlayerHolder()

{2}'stop music
sub StopMusic()

asm

    push ix
    ld a, 0
    ld (_enableMusic), a            ;disable the music player on the interrupt routine
    di                              ;disable interrupts
    call PLY_{0}_STOP               ;stop the player
    ei                              ;enable interrupts
    pop ix

end asm

end sub";

const string basicPlayTemplate = @"sub Play{0}Music()

asm
    
    push ix
    push iy

    ld hl, {1}_START             ;load your song address
    xor a                           ;subtrack 0
    call PLY_{2}_INIT               ;call initialization routine
    
    ld a, 1
    ld (_enableMusic), a            ;enable music on the interrupt routine
    
    pop iy
    pop ix

end asm

end sub
";

CommandLine.Parser.Default.ParseArguments<Options>(args)
    .WithParsed(RunOptions)
    .WithNotParsed(HandleParseError);

static void RunOptions(Options opts)
{
    string tempPath = Directory.CreateTempSubdirectory().FullName;
    
    string[] songs = opts.Songs.Split(':');

    string includes = "";

    List<string> songNames = new List<string>();


    foreach (var song in songs)
    {
        string fileAsm = song + ".asm";
        if (!File.Exists(fileAsm))
        {
            Console.WriteLine($"Cannot find {fileAsm}, aborting.");
            Directory.Delete(tempPath, true);
            return;
        }
        string fileConfig = song + "_playerconfig.asm";
        if (!File.Exists(fileConfig))
        {
            Console.WriteLine($"Cannot find {fileConfig}, aborting.");
            Directory.Delete(tempPath, true);
            return;
        }

        File.Copy(fileAsm, Path.Combine(tempPath, fileAsm));
        File.Copy(fileConfig, Path.Combine(tempPath, fileConfig));

        string name = File.ReadLines(fileAsm).Skip(4).First();
        name = name.Replace("_Start", "");
        songNames.Add(name);

        includes += string.Format(songBuildTemplate, song);
    }

    string buildFile = string.Format(buildTemplate, includes, opts.Player.ToString().ToUpperInvariant(), opts.Player.ToString());

    File.WriteAllText(Path.Combine(tempPath, "build.asm"), buildFile);
    File.WriteAllBytes(Path.Combine(tempPath, "rasm.exe"), RequiredFiles.rasm);
    File.WriteAllBytes(Path.Combine(tempPath, "disark.exe"), RequiredFiles.Disark);

    switch (opts.Player)
    {
        case PlayerTypes.Akg:

            File.WriteAllText(Path.Combine(tempPath, "PlayerAkg.asm"), RequiredFiles.PlayerAkg);
            File.WriteAllText(Path.Combine(tempPath, "PlayerAkg_SoundEffects.asm"), RequiredFiles.PlayerAkg_SoundEffects);

            break;

        case PlayerTypes.Akm:

            File.WriteAllText(Path.Combine(tempPath, "PlayerAkm.asm"), RequiredFiles.PlayerAkm);
            File.WriteAllText(Path.Combine(tempPath, "PlayerAkm_SoundEffects.asm"), RequiredFiles.PlayerAkm_SoundEffects);

            break;
    }

    var proc = Process.Start(new ProcessStartInfo { FileName = Path.Combine(tempPath, "rasm.exe"), WorkingDirectory = tempPath, Arguments = "build.asm -o compiled -s -sl -sq" });

    if (proc == null)
    {
        Console.WriteLine("Cannot start build process, aborting.");
        Directory.Delete(tempPath, true);
        return;
    }

    proc.WaitForExit();

    if (!File.Exists(Path.Combine(tempPath, "compiled.bin")))
    {
        Console.WriteLine("Build failed, aborting.");
        return;
    }

    proc = Process.Start(new ProcessStartInfo { FileName = Path.Combine(tempPath, "disark.exe"), WorkingDirectory = tempPath, Arguments = "compiled.bin decompiled.asm --symbolFile compiled.sym --sourceProfile maxam" });

    if (proc == null)
    {
        Console.WriteLine("Cannot start decompile process, aborting.");
        Directory.Delete(tempPath, true);
        return;
    }

    proc.WaitForExit();

    if (!File.Exists(Path.Combine(tempPath, "decompiled.asm")))
    {
        Console.WriteLine("Decompilation failed, aborting.");
        Directory.Delete(tempPath, true);
        return;
    }

    StringBuilder sb = new StringBuilder();
    var lines = File.ReadAllLines(Path.Combine(tempPath, "decompiled.asm"));

    foreach (var line in lines)
    {
        if (!line.StartsWith(" "))
        {
            string[] parts = line.Split(' ');
            if (parts.Length == 1 || parts[1] != "equ")
                sb.AppendLine(parts[0] + ": " + string.Join(' ', parts.Skip(1)));
            else
                sb.AppendLine(line);
        }
        else
            sb.AppendLine(line);
    }

    if (opts.OutputType == OutputType.Basic)
    {
        string asmContent = sb.ToString();
        sb = new StringBuilder();

        for (int buc = 0; buc < songs.Length; buc++)
        {
            sb.AppendLine(string.Format(basicPlayTemplate, songs[buc], songNames[buc], opts.Player.ToString().ToUpperInvariant()));
        }
        string basicContent = string.Format(basicTemplate, opts.Player.ToString().ToUpperInvariant(), asmContent, sb.ToString());

        File.WriteAllText(opts.OutputFile, basicContent);
    }
    else
        File.WriteAllText(opts.OutputFile, sb.ToString());

    Directory.Delete(tempPath, true);
    Console.WriteLine("Output file created successfully.");

}
static void HandleParseError(IEnumerable<Error> errs)
{
    //handle errors
}
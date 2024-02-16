# ArkosTrackerToZXBasic

This is a little tool that I wrote to ease the integration of Arkos Tracker 2 songs into your ZX Basic projects.

The tool can generate assembler source files compatible with ZXBASM or directly ZX Basic source files that include all the resources to play the songs from your code.

## Preparing the songs

To use the tool the exported songs have a minimal requirements:

* The supported formats are AKG or AKM.
* Every song must include a unique ASM label prefix.
* Songs cannot use samples.
* Songs must be exported along its configuration file.
* Songs must be exported as assembler source.

These are very basic requirements, anything else is up to you :)

## The command line

To use the tool you must provide four parameters: songs, player type, output file and output type.

* Songs: this parameter is specified with '-s' or '--song', this is a list of songs sepparated by semicolon. Each song in this list is the name you used when exported the song but without extension. For example, if you exported 'Intro.asm' and 'InGame.asm' and want to include both then your parameter would be 'Intro:InGame'.
* Player type: this parameter is specified with '-p' or '--player'. The possible values are 'Akg' or 'Akm', for now no more player types are supported.
* Output file: this parameter is specified with '-o' or '--output', the result will be stored with this name.
* Output type: this parameter is specified with '-t' or '--type'. The possible output types are 'Asm' or 'Basic'.

## The assembler output

This format is just the assembled result of the songs plus the player. In order to use it you must know how these work, for more information check the "players" folders of your Arkos Tracker 2 installation.

This is mostly for advanced users that want to manage how the assembly is included, how the interrupts are handled and so on.

## The basic output

This format is the easiest to use. It will generate a basic file with everything ready to be included in your application, simply include the file and you are ready to use it.

### Provided functions

The basic file provides an interrupt handler, a "play" function for each song and a general "stop" function.

#### The interrupt handler

This handler replaces the default spectrum's interrupt handler, it creates a 257 bytes table at $FE00 and a jump routine at $FDFD, this is the safest way to handle an IM2 interrupt in the spectrum and this setup uses the less possible memory. In my experience this could be reduced by removing the table and adding the INTERRUPT_HANDLER address at $FE00 but if the program is used in a real spectrum and the machine has something connected to the expansion port it could cause problems. The routine keeps updated the FRAMES system variable to ensure that any function that uses it is updated, but it does not call to the KEYB handler as it is not needed in ZX Basic.

You must install this handler in order to get the music working as it calls the PLY_XXX_PLAY routine of the player, in order to do it call the "InstallInterruptHandler" function. You can install the handler whenever you want but it must be done BEFORE playing any song or it will not work.

#### The "Play" functions

For each song the tool will create a "Play" function, each one with the name of the song, continuing with the previous example, it would create a "PlayIntroMusic" and "PlayInGameMusic". These names are the ones of the files, not the identifiers given in the export, in this way you can use uppercase names to keep the code tidy and mixed uppercase and lowercase for these names.

#### The "Stop" function

A single function controls when the playback is stopped, calling this function the music will stop and the interrupt will no longer call the PLAY routine.



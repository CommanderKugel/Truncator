# Truncator - a C# Chess Engine
Successor of [Schneckbert](https://github.com/CommanderKugel/Schneckbert/) and hobby-project of Nikolas Windheuser.  
The goal is to optimize for playing strength while maintaining the fun in development.  
Truncator is UCI compliant and thus compatible with modern Chess Interfaces or Matchrunners.

Besides the standard commands `uci`, `isready`, `stop`, `quit`, `ucinewgame`, `position` and `go` there are a couple ucioption's and custom commands.  

### UCI options

`Hash` sets the size of the Transposition Table in mb.  
`Threads` ~sets the number of threads that are used via Lazy SMP while searching~.  
_Threads is broken in release 1.0, always set to 1 or the program crashes!_  
`Move Overhead` sets the minimum overhead to be respected by time mangement.
`SyzygyPath` is used for Syzygy Tablebase probing and makes use of an integrated Fathom dll.
Syzygy probing is only available on windows right now!
`UCI_TbLargest` overwrites the maximum piececount that allows Zyzygy probing. E.g. shrinks available 7 man tables to be only used at 6 pieces or less.
`Softnodes` sets a softnodelimit to be checked between each iteration.
`Hardnodes`sets a hardnodelimit to be checked on every node searched.

### Custom commands

`print` prints an ASCII representation of the current boardstate of the MainThread.  
`bench` runs the custom bench command, that serves the purpose of a finerprint for every engines version. Also used to approximate NPS.  
`perft` runs a custom set of perft positions on a set depth. Used to verify development changes didnt break movegen.  
`pgtoviri` additionally accepts the path to a directory. Reads all PGNs in that directory and creates a `converted.viriformat` file with all games combined.  

### ToDo's

`UCI_Chess960`, `Multipv`, `ponder` and fixed `Threads` commands will come in the near future.  
Be carefull when running Truncator on hyperthreadding - especially when run multithreadded - it has a tendecy to rarely crash during startup, with increasing probability to the number of 'too many' threads.

### Special Thanks

Very Special thanks to:  
the [Stockfish Discord](https://discord.com/invite/GWDRS3kU6R) and the [Engine Programming Discord](https://discord.com/invite/F6W6mMsTGN),  
for allowing conversations and a place to get high quality advice (and shitposts), as most usefull information is hidden away in engines or the heads of their developers.
  
The [Weiss engine and developer](https://github.com/TerjeKir/weiss), the [Ethereal engine and developer](https://github.com/AndyGrant/Ethereal), the [Stormphrax engine and developer](https://github.com/Ciekce/Stormphrax),
and [Stockfish engine and developers](https://github.com/official-stockfish/Stockfish/), because sometimes you just need a reference on how an implementation might look like, e.g. probcut and Syzygy probing inplementations.  
  
The [Openbench framework and its Author Andy Grant](https://github.com/AndyGrant/OpenBench) for allowing me to use destributed sprt's, spsa's and datagenning,
the [Fathom Syzygy probing Code and developers](https://github.com/jdart1/Fathom) to allow Truncator to have Tablebase probing,
and the [Bullet author](https://github.com/jw1912/bullet) to allow me to efficiently train NNUE's.

It's save to say, that neither Truncator, nor Schneckbert would have come into existance without the open-mindedness and open-source culture of the chess engine dev community.  
It's a hard path to walk alone but luckily nobody has to!  
Whenever i asked a question, someone was willing to answere :)

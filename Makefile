EXE = Truncator

ifeq ($(OS),Windows_NT)
	SRC := Truncator.exe
	DEST := $(EXE).exe
else
	SRC := Truncator
	DEST := $(EXE)
endif

all:
	dotnet publish -c Release Truncator/ --output Truncator/bin/OpenbenchBin
	mv Truncator/bin/OpenbenchBin/$(SRC) ./$(DEST)
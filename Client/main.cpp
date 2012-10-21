#include "hollyclient.h"

#include <stdio.h>
#include <stdlib.h>

#ifndef WIN32
#include <unistd.h>

#else
#include "pgetopt/pgetopt.h"
#define getopt(a,b,c) pgetopt(a,b,c)
#define optarg poptarg

#include <io.h>
#define read(a,b,c) _read(a,b,c)

#include <windows.h>

#endif

bool started = false;

int main(int argc, char* argv[]){
	SetDllDirectory("D:\\devel\\github\\libfreenect-build\\lib\\Debug");

	char *server = "127.0.0.1";
	char *s_port = 0;
	bool help = false;
	bool forever = true;
	int c;
	bool reconnect = true;
	bool audiotest = false;

	//getargs
	while ((c = getopt(argc, argv, "ahs:p:?f")) != -1 && !help){
		switch (c){
			default:
			case 'h':
			case '?':
				help = true;
				break;
			case 's':
				server = optarg;
				break;
			case 'f':
				forever = true;
			case 'p':
				s_port = optarg;
				break;
			case 'a':
				audiotest = true;
				break;
		}
	}

	if(audiotest){
		audioout_setup();		
		char *buffer = (char*)malloc(32 * 2);
		while(true){
			if(read(0, buffer, 32*2)!=-1)
				audioout_write(buffer, 32);
		}	


		printf("Run complete\n");
		exit(1);
	}

	if(s_port == 0) s_port = "31337";
   if(server == 0) help = true;

	if(help){
		printf("hollyclient: [-h?] -s <server> [-p <port>] -f(orever)\n");
		return 1;
	}

	printf("Initialising Audio\n");
	//intialise audio
	if(audio_setup()==-1){
		return -1;
	}
	if(audioout_setup()!=1){
		return -1;
	}

	printf("Initialising Net\n");
	//initialise net
	if(net_setup(server, s_port)==-1){
		return -1;
	}

	printf("Connecting...\n");
	//connect

	while(true){
		if(reconnect){
			while(net_connect()!=0){
				if(!forever) return -1;
			}
			reconnect = false;
		}
		if(started){
			audio_run();
			if(!net_poll()){
				if(!forever) break;
				reconnect = true;
			}
		}
		else {
			if(!net_awaitcommand()){
				if(!forever) break;
				reconnect = true;
			}
		}

	}

	return 1;
}



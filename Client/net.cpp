#include "hollyclient.h"

#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <netinet/in.h>
#include <netdb.h>
#include <poll.h>
#include <unistd.h>


char server[255];
unsigned short port;
int sockfd;

extern bool started;

const char *msg_hello = "HELLO";
const char *msg_ready = "RDY";
const char *msg_start = "START";
const char *msg_stop = "STOP";
const char *msg_getbaseline = "GETBASE";
const char *msg_rptbaseline = "BASELINE";
const char *msg_config = "CONFIG";
const char *msg_data = "DATA";
const char *msg_audioout = "AUDIOOUT";

struct pollfd toPoll;

int send_msg(char *data, unsigned int len);
char *rcv_msg(int *len);


int min(int a, int b){
	if(a<b) return a;
	else return b;
}

int net_setup(char *s_server, char *s_port){
	strncpy(server, s_server, 254);
	int i = strtol(s_port, 0, 0);
	if(i<0 || i>65535){
		printf("ERR: port outside range\n");
		return -1;
	}
	port = i;
	return 0;
}

int net_connect(){
	struct sockaddr_in sin;

	sockfd = socket(AF_INET, SOCK_STREAM, 0);
	if(sockfd<0){
		printf("ERR: socket failed\n");
		return -1;
	}
	memset(&sin, 1, sizeof(struct sockaddr_in));
	sin.sin_family = AF_INET;
	sin.sin_port = htons(port);
	if(inet_aton(server, &(sin.sin_addr))==0){
		printf("ERR: unknown host %s\n", server);
	}

	if(connect(sockfd, (struct sockaddr*)&sin, sizeof(struct sockaddr_in))<0){
		printf("ERR: Couldn't connect: %d\n", errno);
		close(sockfd);
		sockfd = -1;
		return -1;
	}

	//send hello
	printf("Sending Hello\n");
	if(send_msg((char*)msg_hello, strlen(msg_hello))!=0){
		return -1;
	}
	
	//get request
	printf("Getting request\n");
	int len;
	char *buf = rcv_msg(&len);
	if(buf==0 || strncmp(buf, msg_config, strlen(msg_config))!=0){
		//Config message not received
		printf("ERR: Config message not received\n");
		if(buf==0) printf("   returned 0\n");
		else printf("   received: %s\n", buf);
		if(buf!=0) free(buf);
		return -1;
	}
	if(buf!=0) free(buf);

	//send ready
	printf("Sending ready\n");
	if(send_msg((char*)msg_ready, strlen(msg_ready))!=0){
		return -1;
	}

	toPoll.fd = sockfd;
	toPoll.events  = POLLIN;

	started = false;

	return 0;
}

int fake = 0;
int net_audiodata(int numsamples, int32_t *data){
	unsigned int len = numsamples*sizeof(int32_t) + strlen(msg_data);
	char * buf = (char*)malloc(len);
	if(buf == 0){
		printf("ERR: net_audiodata malloc failed\n");
		return -1;
	}
	strcpy(buf, msg_data);
	memcpy(buf+strlen(msg_data), data, numsamples*sizeof(int32_t));
	//for(int i=0;i<numsamples;i++,fake++){
	//	memcpy(((int*)(buf+strlen(msg_data)))+i, &fake, 4);
	//}
	send_msg(buf, len);
	free(buf);
	return 1;
}

bool recv_if_possible(int timeout);
bool net_poll(){
	return recv_if_possible(0);
}

bool net_awaitcommand(){
	return recv_if_possible(-1);
}

bool recv_if_possible(int timeout){
	//printf("recv_if_possible\n");
	toPoll.revents = 0;
	int i = poll(&toPoll, 1, timeout);
	if(i==-1){
		close(sockfd);
		sockfd = -1;
		return false;
	}
	else if(i==0){
		return true;
	}
	if(toPoll.revents & (POLLRDHUP | POLLERR | POLLHUP)){
		close(sockfd);
		sockfd=-1;
		return false;
	}


	//Handle Commands
	int len;
	char *msg = rcv_msg(&len);
	if(msg==0) return false;

	if(strncmp(msg, msg_start, min(len, strlen(msg_start)))==0){
		printf("Started\n");
		started = true;
	}
	else if(strncmp(msg, msg_stop, min(len, strlen(msg_stop)))==0){
		printf("Stopped\n");
		started = false;
	}
	else if(strncmp(msg, msg_getbaseline, min(len, strlen(msg_getbaseline)))==0){
		//TODO
	}
	else if(strncmp(msg, msg_audioout, min(len, strlen(msg_audioout)))==0){
		printf("Received audio data\n");
		char *data = msg+strlen(msg_audioout);
		int nFrames = len-strlen(msg_audioout);
		nFrames /= 2; //2 bytes per frame
		audioout_write(data, nFrames);
	}
	else {
		for(int i=0;i<len;i++)
			if((unsigned char)msg[i]<0x20 
			   || (unsigned char)msg[i]>0x7F)
				msg[i] = 0x5F;
		printf("Unknown message: %s\n", msg);
	}

	if(msg!=0) free(msg);
	return true;
}







int send_msg(char *data, unsigned int len){
	//printf("SENDING %d byte packet\n", len);
	unsigned long sent;
	char *buf = (char*)malloc(len + sizeof(len));
	if(buf==0){
		printf("ERR: malloc failed\n");
		return -1;
	}
	*(int*)buf = htonl(len);
	memcpy(buf+sizeof(len), data, len);

	sent = send(sockfd, buf, len+sizeof(len), 0);
	if(sent != len+sizeof(len)){
		printf("ERR: send was short\n");
		close(sockfd);
		sockfd = -1;
		return -1;
	}
	free(buf);
	return 0;
}

char *rcv_msg(int *len){
	if(sockfd==-1){
		printf("ERR: Socket closed/invalid\n");
		return 0;
	}
	if(len==0){
		printf("ERR: Invalid len\n");
		return 0;
	}

	int i = recv(sockfd, len, sizeof(unsigned long), MSG_WAITALL);
	if(i!=sizeof(unsigned long)){
		printf("ERR: Couldn't get header\n");
		return 0;
	}
	char *ret = (char*)malloc(*len);
	if(ret==0){
		printf("ERR: Malloc in recv failed\n");
		return 0;
	}
	i = recv(sockfd, ret, *len, MSG_WAITALL);
	if(i!=*len){
		printf("ERR: recv2 failed\n");
		return 0;
	}

	return ret;
}




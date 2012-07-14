
int net_setup(char*, char*);
int net_connect();
int net_audiodata(int numsamples, int *data);
bool net_poll();
bool net_awaitcommand();

int audio_setup();
void audio_run();

int audioout_setup();
int audioout_write(char *data, int nframes);
int audioout_close();


#include "hollyclient.h"
#include <stdio.h>

#define ALSA_PCM_NEW_HW_PARAMS_API
#include <alsa/asoundlib.h>

#include <pthread.h>

#define SLEEPFRAMES	256


snd_pcm_t *handle;
pthread_t writeThread;

void* audioout_setup_threadproc(void *args);


int _audioout_setup_sw(){
	snd_pcm_sw_params_t *sw_params;
	int err;

	if((err = snd_pcm_sw_params_malloc(&sw_params))<0){
		printf("ERR: malloc failed: %s\n", snd_strerror(err));
		exit(-1);
	}
	if ((err = snd_pcm_sw_params_current (handle, sw_params)) < 0) {
		fprintf (stderr, "cannot initialize software parameters structure (%s)\n",
			 snd_strerror (err));
		exit (1);
	}
	if ((err = snd_pcm_sw_params_set_avail_min (handle, sw_params, SLEEPFRAMES)) < 0) {
		fprintf (stderr, "cannot set minimum available count (%s)\n",
		 snd_strerror (err));
		exit (1);
	}
	if ((err = snd_pcm_sw_params_set_start_threshold (handle, sw_params, 0U)) < 0) {
		fprintf (stderr, "cannot set start mode (%s)\n",
		 snd_strerror (err));
		exit (1);
	}
	if ((err = snd_pcm_sw_params (handle, sw_params)) < 0) {
		fprintf (stderr, "cannot set software parameters (%s)\n",
		 snd_strerror (err));
		exit (1);
	}
	return 1;
}


snd_pcm_hw_params_t *_audioout_setup_hw(){
	int rc;
	snd_pcm_hw_params_t *params;
	unsigned int val;
	int dir;

	snd_pcm_hw_params_alloca(&params);
	rc = snd_pcm_hw_params_any(handle, params);
	if(rc < 0){
		printf("Couldn't configure pcm(any): %s\n", snd_strerror(rc));
		exit(1);
	}
	unsigned int resample = 1;
	rc = snd_pcm_hw_params_set_rate_resample(handle, params, resample);
	if(rc < 0){
		printf("Couldn't configure pcm(resample): %s\n", snd_strerror(rc));
		exit(1);
	}
	rc = snd_pcm_hw_params_set_access(handle, params, SND_PCM_ACCESS_RW_INTERLEAVED);
	if(rc < 0){
		printf("Couldn't configure pcm(access): %s\n", snd_strerror(rc));
		exit(1);
	}
	rc = snd_pcm_hw_params_set_format(handle, params, SND_PCM_FORMAT_S16_LE);
	if(rc < 0){
		printf("Couldn't configure pcm(format): %s\n", snd_strerror(rc));
		exit(1);
	}
	rc = snd_pcm_hw_params_set_channels(handle, params, 1);
	if(rc < 0){
		printf("Couldn't configure pcm: %s\n", snd_strerror(rc));
		exit(1);
	}
	val = 16000;
	rc = snd_pcm_hw_params_set_rate_near(handle, params, &val, &dir);
	if(rc < 0){
		printf("Couldn't configure pcm: %s\n", snd_strerror(rc));
		exit(1);
	}
	rc = snd_pcm_hw_params(handle, params);
	if(rc < 0){
		printf("Couldn't configure pcm: %s\n", snd_strerror(rc));
		exit(1);
	}
//	snd_pcm_hw_params_free(params);
	return params;
}

int _audioout_setup_printhw(snd_pcm_hw_params_t *params){
	unsigned int val;
	int dir;
	snd_pcm_uframes_t frames;


	 //display tunerAudio card parameters
        printf("tunerAudio card handler name = %s\n", snd_pcm_name(handle));
        printf("tunerAudio pcm state = %s\n",
            snd_pcm_state_name(snd_pcm_state(handle)));

        snd_pcm_hw_params_get_access(params, (snd_pcm_access_t *) &val);
        printf("tunerAudio access type = %s\n", snd_pcm_access_name((snd_pcm_access_t)val));

        snd_pcm_hw_params_get_format(params, (snd_pcm_format_t*)&val);
        printf("tunerAudio format = %s (%s)\n", snd_pcm_format_name((snd_pcm_format_t) val),
            snd_pcm_format_description((snd_pcm_format_t) val));

        snd_pcm_hw_params_get_format(params, (snd_pcm_format_t*)&val);
         printf("format = '%s' (%s)\n", snd_pcm_format_name((snd_pcm_format_t)val),
             snd_pcm_format_description((snd_pcm_format_t)val));

        snd_pcm_hw_params_get_subformat(params, (snd_pcm_subformat_t *)&val);
         printf("subformat = '%s' (%s)\n", snd_pcm_subformat_name((snd_pcm_subformat_t)val),
             snd_pcm_subformat_description((snd_pcm_subformat_t)val));

         snd_pcm_hw_params_get_channels(params, &val);
         printf("channels = %d\n", val);

         snd_pcm_hw_params_get_rate(params, &val, &dir);
         printf("rate = %d bps\n", val);

         snd_pcm_hw_params_get_period_time(params, &val, &dir);
         printf("period time = %d us\n", val);

         snd_pcm_hw_params_get_period_size(params, &frames, &dir);
         printf("period size = %d frames\n", (int)frames);

         snd_pcm_hw_params_get_buffer_time(params, &val, &dir);
         printf("buffer time = %d us\n", val);

         snd_pcm_hw_params_get_buffer_size(params,(snd_pcm_uframes_t *) &val);
         printf("buffer size = %d frames\n", val);

         snd_pcm_hw_params_get_periods(params, &val, &dir);
         printf("periods per buffer = %d frames\n", val);
	
	return 1;
}

int audioout_setup(){
	int ipret;
	int rc;



	ipret = pthread_create(&writeThread, NULL, audioout_setup_threadproc, NULL);

	return 1;
}

struct LL {
	struct LL* next;
	char *buf;
	unsigned int buflen;
};

unsigned int total_len = 0;
struct LL *head;
pthread_mutex_t head_mutex = PTHREAD_MUTEX_INITIALIZER;
pthread_mutex_t dataAvail_mutex = PTHREAD_MUTEX_INITIALIZER;
pthread_cond_t  dataAvail_cond = PTHREAD_COND_INITIALIZER;

void AddBlock(char *blob, unsigned int len){
	if(len==0) return;
	struct LL *block = (struct LL*)malloc(sizeof(struct LL));
	if(block==0){
		printf("malloc failed\n");
		exit(1);
	}
	block->next = (struct LL*)0;
	block->buf = (char*)malloc(len);
	if(block->buf==0){
		printf("malloc2 failed\n");
		exit(1);
	}
	block->buflen = len;
	memcpy(block->buf, blob, len);

	//printf("AddBlock: mutex_lock(head)\n");
	pthread_mutex_lock(&head_mutex);
	//printf("   AddBlock: mutex_lock(head) success\n");
	total_len += len;
	if(head==0) head = block;
	else {
		struct LL *tmp = head;
		while(tmp->next != (struct LL*)0) 
			tmp = tmp->next;
		tmp->next = block;
	}

	//printf("AddBlock: mutex_unlock(head)\n");
	pthread_mutex_unlock(&head_mutex);
	//printf("   AddBlock: mutex_unlock(head)\n");
	
	//printf("AddBlock: mutex_lock(data)\n");
	pthread_mutex_lock(&dataAvail_mutex);
	//printf("   AddBlock: mutex_lock(data) success\n");
	pthread_cond_signal(&dataAvail_cond);
	//printf("AddBlock: mutex_unlock(data)\n");
	pthread_mutex_unlock(&dataAvail_mutex);
	//printf("   AddBlock: mutex_unlock(data) success\n");
}

char *GetBlock(int nframes){
	if(nframes < 0) return (char*)0;

	//printf("GetBlock: mutex_lock(head)\n");
	pthread_mutex_lock(&head_mutex);
	//printf("   GetBlock: mutex_lock(head) success\n");
	if(total_len < (unsigned int)(nframes*2)){
		//printf("GetBlock: mutex_unlock(head)\n");
		pthread_mutex_unlock(&head_mutex);
		//printf("    GetBlock: mutex_unlock(head) success\n");
		return (char*)0;
	}

	char *ret = (char*)malloc(nframes * 2);
	int got = 0;
	unsigned int cp = 0;
	struct LL *tmp = head;
	struct LL *tmp2;
	while(got<(nframes*2)){
		if(tmp->buflen > ((unsigned int)nframes*2)-got)
			cp = (nframes*2)-got;
		else
			cp = tmp->buflen;
		memcpy(&(ret[got]), tmp->buf, cp);
		got += cp;
		if(cp < tmp->buflen){
			//still data in block
			memmove(tmp->buf, &(tmp->buf[cp]), tmp->buflen-cp);
			tmp->buflen -= cp;
		}
		else {
			//full block has been used
			tmp2 = tmp;
			tmp = tmp->next;
			head = tmp;
			free(tmp2->buf);
			free(tmp2);
		}

	}
	total_len -= nframes*2;
	//printf("GetBlock: mutex_unlock(head)\n");
	pthread_mutex_unlock(&head_mutex);
	//printf("    GetBlock: mutex_unlock(head) success\n");

	return ret;
}





void* audioout_setup_threadproc(void *args){
	int err;
	int frames_to_deliver;
	int rc;
	bool should_sleep = false;

	rc = snd_pcm_open(&handle, "default", SND_PCM_STREAM_PLAYBACK, 0);
	if(rc<0){
		printf("Couldn't open pcm: %s\n", snd_strerror(rc));
		exit(1);
	}

	snd_pcm_hw_params_t *params;
	params = _audioout_setup_hw();
	_audioout_setup_sw();
	//_audioout_setup_printhw(params);

	/* the interface will interrupt the kernel every 4096 frames, and ALSA
	 * 		   will wake up this program very soon after that.
	 * 		   		*/
	if ((err = snd_pcm_prepare (handle)) < 0) {
		fprintf (stderr, "cannot prepare audio interface for use (%s)\n",
		 snd_strerror (err));
		exit (1);
	}
	while (1) {
		if(total_len==0 || should_sleep){
			should_sleep = false;
			//nothing to write, let's sleep
			//printf("threadproc: mutex_lock(data)\n");
			pthread_mutex_lock(&dataAvail_mutex);
			//printf("    threadproc: mutex_lock(data) success\n");
			pthread_cond_wait(&dataAvail_cond, &dataAvail_mutex);
			//printf("threadproc: mutex_unlock(data)\n");
			pthread_mutex_unlock(&dataAvail_mutex);
			//printf("    threadproc: mutex_unlock(data) success\n");
		}

		//try to write
		if ((err = snd_pcm_wait (handle, 1000)) < 0) {
			if(err == -EPIPE){
				printf("poll underrun\n");
				snd_pcm_prepare(handle);
				continue;
			}
			else {	
		        	printf ("poll failed (%s)\n", snd_strerror (err));
			}
		        break;
		}	           
		if ((frames_to_deliver = snd_pcm_avail_update (handle)) < 0) {
			if (frames_to_deliver == -EPIPE) {
				fprintf (stderr, "an xrun occured\n");
				break;
			} else {
				fprintf (stderr, "unknown ALSA avail update return value (%d)\n",  frames_to_deliver);
				break;
			}
		}
		
		frames_to_deliver = frames_to_deliver > SLEEPFRAMES ? SLEEPFRAMES : frames_to_deliver;
		char *buf = GetBlock(frames_to_deliver);
		if(buf==(char*)0){
			should_sleep = true;
			continue;
		}
		if((err = snd_pcm_writei(handle, buf, frames_to_deliver))<frames_to_deliver){
			if(err == -EPIPE){
				printf("Underrun");
				snd_pcm_prepare(handle);
			}
			else {
				printf("ERR: write failed: %s\n", snd_strerror(err));
			}
		}
		free(buf);
	}
	snd_pcm_close (handle);
	exit(1);
	return (void*)0;
}

int audioout_write(char *data, int nframes){
	AddBlock(data, nframes * 2);
	return 1;
}

int audioout_close(){
	//kill writeThread
	void *res;
	int err = pthread_cancel(writeThread);
	if(err!=0){
		printf("ERR: Couldn't cancel writeThread: %d\n", err);
	}
	err = pthread_join(writeThread, &res);
	if(err!=0){
		printf("ERR: Couldn't join writeThread: %d\n", err);
	}

	snd_pcm_drain(handle);
	snd_pcm_close(handle);

	return 0;
}


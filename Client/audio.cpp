#include "hollyclient.h"
#include <stdio.h>
#include <libfreenect/libfreenect.h>
#include <libfreenect/libfreenect-audio.h>
#include <signal.h>


extern bool started;

static freenect_context* f_ctx;
static freenect_device* f_dev;

void in_callback(freenect_device* dev, int num_samples,
                 int32_t* mic1, int32_t* mic2,
                 int32_t* mic3, int32_t* mic4,
                 int16_t* cancelled, void *unknown) {
	//printf("%d samples received.\n", num_samples);
	net_audiodata(num_samples, mic2);
}


int audio_setup(){
	if (freenect_init(&f_ctx, NULL) < 0) {
		printf("freenect_init() failed\n");
		return -1;
	}
	freenect_set_log_level(f_ctx, FREENECT_LOG_INFO);
	freenect_select_subdevices(f_ctx, FREENECT_DEVICE_AUDIO);
	int nr_devices = freenect_num_devices (f_ctx);
	printf ("Number of devices found: %d\n", nr_devices);
	if (nr_devices < 1)
		return -1;

	int user_device_number = 0;
	if (freenect_open_device(f_ctx, &f_dev, user_device_number) < 0) {
		printf("Could not open device\n");
		return 1;
	}
	freenect_set_audio_in_callback(f_dev, in_callback);
	freenect_start_audio(f_dev);

	return 0;
}



void audio_run(){
	//printf("audio_run");
	freenect_process_events(f_ctx);
	
}


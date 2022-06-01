package h264_test

import (
	"io/ioutil"
	"os"
	"testing"

	"github.com/karashiiro/prima-sentinel/detection/h264"
)

func Test_DoesVideoResolutionChange_Crash(t *testing.T) {
	handle, err := os.Open("./resolution_test.mp4")
	if err != nil {
		t.Fatal(err)
	}

	data, err := ioutil.ReadAll(handle)
	if err != nil {
		t.Fatal(err)
	}

	resolutionChanges, err := h264.DoesVideoResolutionChange(data)
	if err != nil {
		t.Fatal(err)
	}

	if !resolutionChanges {
		t.Fail()
	}
}

func Test_DoesVideoResolutionChange_NoCrash(t *testing.T) {
	handle, err := os.Open("./color_space_test.mp4")
	if err != nil {
		t.Fatal(err)
	}

	data, err := ioutil.ReadAll(handle)
	if err != nil {
		t.Fatal(err)
	}

	resolutionChanges, err := h264.DoesVideoResolutionChange(data)
	if err != nil {
		t.Fatal(err)
	}

	if resolutionChanges {
		t.Fail()
	}
}

package h264_test

import (
	"io/ioutil"
	"os"
	"testing"

	"github.com/karashiiro/prima-sentinel/detection/h264"
)

func Test_DoesVideoColorSpaceChange_Crash(t *testing.T) {
	handle, err := os.Open("./color_space_test_crash.mp4")
	if err != nil {
		t.Fatal(err)
	}

	data, err := ioutil.ReadAll(handle)
	if err != nil {
		t.Fatal(err)
	}

	colorSpaceChanges, err := h264.DoesVideoColorSpaceChange(data)
	if err != nil {
		t.Fatal(err)
	}

	if !colorSpaceChanges {
		t.Fail()
	}
}

func Test_DoesVideoColorSpaceChange_NoCrash(t *testing.T) {
	handle, err := os.Open("./color_space_test.mp4")
	if err != nil {
		t.Fatal(err)
	}

	data, err := ioutil.ReadAll(handle)
	if err != nil {
		t.Fatal(err)
	}

	colorSpaceChanges, err := h264.DoesVideoColorSpaceChange(data)
	if err != nil {
		t.Fatal(err)
	}

	if colorSpaceChanges {
		t.Fail()
	}
}

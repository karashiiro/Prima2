package h264

import (
	"bytes"
	"os/exec"
	"strings"

	"github.com/karashiiro/prima-sentinel/ephemerals"
)

func DoesVideoColorSpaceChange(video []byte) (bool, error) {
	// Copy contents to temporary file
	tmp, err := ephemerals.CreateTemporaryFile(video)
	if err != nil {
		return false, err
	}
	defer tmp.Unlink()

	// Export the first and last frames.
	// While ideally we'd export all of them, it takes a while
	// for ffmpeg to run.
	firstFrame, err := ephemerals.CreateTemporaryFile(nil, "jpg")
	if err != nil {
		return false, err
	}
	firstFrame.Unlink()
	defer firstFrame.Unlink() // ffmpeg recreates the file later

	lastFrame, err := ephemerals.CreateTemporaryFile(nil, "jpg")
	if err != nil {
		return false, err
	}
	lastFrame.Unlink()
	defer lastFrame.Unlink()

	firstCmd := exec.Command("ffmpeg", "-i", tmp.File.Name(), "-vframes", "1", "-q:v", "1", firstFrame.File.Name())
	if err := firstCmd.Run(); err != nil {
		return false, err
	}

	lastCmd := exec.Command("ffmpeg", "-sseof", "-3", "-i", tmp.File.Name(), "-update", "1", "-q:v", "1", lastFrame.File.Name())
	if err := lastCmd.Run(); err != nil {
		return false, err
	}

	// Do analysis
	firstFrameAnalysis := exec.Command("ffprobe", "-i", firstFrame.File.Name())
	var firstStderr bytes.Buffer
	firstFrameAnalysis.Stderr = &firstStderr
	if err := firstFrameAnalysis.Run(); err != nil {
		return false, err
	}

	lastFrameAnalysis := exec.Command("ffprobe", "-i", lastFrame.File.Name())
	var lastStderr bytes.Buffer
	lastFrameAnalysis.Stderr = &lastStderr
	if err := lastFrameAnalysis.Run(); err != nil {
		return false, err
	}

	// Compare last lines of each ffprobe output
	firstOutput := strings.Split(strings.TrimSpace(firstStderr.String()), "\n")
	lastOutput := strings.Split(strings.TrimSpace(lastStderr.String()), "\n")
	return firstOutput[len(firstOutput)-1] != lastOutput[len(lastOutput)-1], nil
}

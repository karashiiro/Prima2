package h264

import (
	"bufio"
	"log"
	"os/exec"
	"strconv"
	"strings"

	"github.com/karashiiro/prima-sentinel/ephemerals"
)

const EightK = 7680 * 4320

func DoesVideoResolutionChange(video []byte) (bool, error) {
	// Copy contents to temporary file
	tmp, err := ephemerals.CreateTemporaryFile(video)
	if err != nil {
		return false, err
	}
	defer tmp.Unlink()

	// Run ffprobe
	cmd := exec.Command("ffprobe", "-v", "error", "-show_entries", "frame=width,height", "-select_streams", "v", "-of", "csv=p=0", tmp.File.Name())

	stdout, err := cmd.StdoutPipe()
	if err != nil {
		return false, err
	}

	if err := cmd.Start(); err != nil {
		return false, err
	}

	var outputRows []string
	scanner := bufio.NewScanner(stdout)
	for scanner.Scan() {
		outputRows = append(outputRows, scanner.Text())
	}

	if err := cmd.Wait(); err != nil {
		return false, err
	}

	// Do analysis of results
	if len(outputRows) < 2 {
		return false, nil
	}

	lastRow := outputRows[len(outputRows)-3]
	for i := len(outputRows) - 4; i >= 0; i-- {
		if outputRows[i] == "" {
			continue
		}

		if outputRows[i] != lastRow {
			changedStr := strings.Split(outputRows[i], ",")
			finalStr := strings.Split(lastRow, ",")

			changedX, err := strconv.ParseUint(changedStr[0], 10, 64)
			if err != nil {
				log.Println(err)
				continue
			}

			changedY, err := strconv.ParseUint(changedStr[1], 10, 64)
			if err != nil {
				log.Println(err)
				continue
			}

			finalX, err := strconv.ParseUint(finalStr[0], 10, 64)
			if err != nil {
				log.Println(err)
				continue
			}

			finalY, err := strconv.ParseUint(finalStr[1], 10, 64)
			if err != nil {
				log.Println(err)
				continue
			}

			if changedX*changedY <= EightK && finalX*finalY <= EightK {
				continue
			}

			return true, nil
		}
	}

	return false, nil
}

package img

import (
	_ "embed"
	"strings"
)

var (
	//go:embed vb_sig.bin
	vbsig string
)

// DoesImageTriggerWindowsDefender returns true if the provided image contains
// some specific VBScript code designed to trigger Windows Defender.
func DoesImageTriggerWindowsDefender(image []byte) (bool, error) {
	imgStr := string(image)
	return strings.Contains(imgStr, vbsig), nil
}

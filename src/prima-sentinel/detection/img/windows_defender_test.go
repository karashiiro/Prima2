package img_test

import (
	_ "embed"
	"io/ioutil"
	"net/http"
	"testing"

	"github.com/karashiiro/prima-sentinel/detection/img"
)

const image = "https://i.imgur.com/yBpdvtN.png"

var (
	//go:embed vb_sig.bin
	vbsig []byte
)

func Test_WindowsDefender_Flagged(t *testing.T) {
	res, err := http.Get(image)
	if err != nil {
		t.Fatal(err)
	}
	defer res.Body.Close()

	data, err := ioutil.ReadAll(res.Body)
	if err != nil {
		t.Fatal(err)
	}
	data = append(data, vbsig...)

	willBeFlagged, err := img.DoesImageTriggerWindowsDefender(data)
	if err != nil {
		t.Fatal(err)
	}

	if !willBeFlagged {
		t.Fail()
	}
}

func Test_WindowsDefender_NotFlagged(t *testing.T) {
	res, err := http.Get(image)
	if err != nil {
		t.Fatal(err)
	}
	defer res.Body.Close()

	data, err := ioutil.ReadAll(res.Body)
	if err != nil {
		t.Fatal(err)
	}

	willBeFlagged, err := img.DoesImageTriggerWindowsDefender(data)
	if err != nil {
		t.Fatal(err)
	}

	if willBeFlagged {
		t.Fail()
	}
}

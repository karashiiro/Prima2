package captchagen

import (
	"bytes"
	"io"
	"sync"

	"github.com/dchest/captcha"
)

var idMap = map[string]string{}
var mtx = sync.Mutex{}

func Generate(id string) (io.Reader, error) {
	// Create a state entry for the new image
	mtx.Lock()
	captchaId := captcha.NewLen(6)
	idMap[id] = captchaId
	mtx.Unlock()

	// Generate the image
	buf := bytes.Buffer{}
	err := captcha.WriteImage(&buf, captchaId, 600, 400)
	if err != nil {
		return nil, err
	}

	return &buf, nil
}

func Verify(id, test string) bool {
	mtx.Lock()
	captchaId, ok := idMap[id]
	if !ok {
		mtx.Unlock()
		return false
	}
	delete(idMap, id)
	mtx.Unlock()

	return captcha.VerifyString(captchaId, test)
}

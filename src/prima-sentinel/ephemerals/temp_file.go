package ephemerals

import (
	"io/ioutil"
	"os"
)

type TemporaryFile struct {
	File *os.File
}

func CreateTemporaryFile(data []byte, extension ...string) (*TemporaryFile, error) {
	pattern := "sentinel_"
	if extension != nil {
		pattern += "*." + extension[0]
	}

	tmp, err := ioutil.TempFile(os.TempDir(), pattern)
	if err != nil {
		return nil, err
	}

	if _, err = tmp.Write(data); err != nil {
		return nil, err
	}

	if err = tmp.Close(); err != nil {
		return nil, err
	}

	return &TemporaryFile{
		File: tmp,
	}, nil
}

func (t *TemporaryFile) Unlink() error {
	if err := os.Remove(t.File.Name()); err != nil {
		return err
	}
	return nil
}

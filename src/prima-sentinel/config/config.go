package config

// You shouldn't hardcode configuration like this, but...

const CEMGuildID = "550702475112480769"
const PrimaGuildID = "550910482194890781"

var AcceptedGuildIDs = []string{CEMGuildID, PrimaGuildID}
var ReportChannelIDs = map[string]string{
	CEMGuildID:   "569310198594928660",
	PrimaGuildID: "675741082159415317",
}

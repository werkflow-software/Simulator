using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Werkflow.OpcUaSimulator.Core.Models;

public static class DefaultErrorMessages
{
	public static List<string> Create()
	{
		int num = 10;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		span[0] = "Not-Halt aktiv";
		span[1] = "Material fehlt";
		span[2] = "Schutzbereich geöffnet";
		span[3] = "Werkzeugprüfung erforderlich";
		span[4] = "Maschinenstörung";
		span[5] = "Antrieb nicht bereit";
		span[6] = "Netzwerkkommunikation unterbrochen";
		span[7] = "Auftrag kann nicht geladen werden";
		span[8] = "Bedienereingriff erforderlich";
		span[9] = "Temperaturgrenze erreicht";
		return list;
	}
}

﻿using XIVAPI4.Services.Readers;
using XIVAPI4.Services.Readers.Lumina;

namespace Microsoft.Extensions.DependencyInjection {
	public static class ReaderServiceExtensions {
		public static IServiceCollection AddReaders(this IServiceCollection services) {
			// TODO: Register lumina here
			services.AddSingleton<IReader, LuminaReader>();

			return services;
		}
	}
}

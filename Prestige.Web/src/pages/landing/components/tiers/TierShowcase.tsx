import React, { useState } from "react";
import bronzeTier from "@/assets/tiers/bronze.png";
import silverTier from "@/assets/tiers/silver.png";
import goldTier from "@/assets/tiers/gold.png";
import diamondTier from "@/assets/tiers/diamond.png";
import emeraldTier from "@/assets/tiers/emerald.png";
import sapphireTier from "@/assets/tiers/sapphire.png";
import garnetTier from "@/assets/tiers/garnet.png";
import opalTier from "@/assets/tiers/opal.png";
import peridotTier from "@/assets/tiers/peridot.png";
import jetTier from "@/assets/tiers/jet.png";
import darkmatterTier from "@/assets/tiers/darkmatter.png";

type TierType = 'track' | 'album' | 'artist';

const TierShowcase: React.FC = () => {
	const [selectedType, setSelectedType] = useState<TierType>('track');
	
	const tierData = {
		track: [
			{ name: "Bronze", image: bronzeTier, minutes: "1h", description: "Your journey begins", color: "#CD7F32" },
			{ name: "Silver", image: silverTier, minutes: "2h", description: "Building dedication", color: "#C0C0C0" },
			{ name: "Peridot", image: peridotTier, minutes: "3h", description: "Growing passion", color: "#E6E200" },
			{ name: "Gold", image: goldTier, minutes: "6h", description: "True appreciation", color: "#FFD700" },
			{ name: "Emerald", image: emeraldTier, minutes: "10h", description: "Deep connection", color: "#50C878" },
			{ name: "Sapphire", image: sapphireTier, minutes: "20h", description: "Passionate listener", color: "#0F52BA" },
			{ name: "Garnet", image: garnetTier, minutes: "30h", description: "Unwavering loyalty", color: "#722F37" },
			{ name: "Jet", image: jetTier, minutes: "40h", description: "Extraordinary bond", color: "#343434" },
			{ name: "Diamond", image: diamondTier, minutes: "60h", description: "Exceptional taste", color: "#B9F2FF" },
			{ name: "Opal", image: opalTier, minutes: "120h", description: "Rare devotion", color: "#A8C3BC" },
			{ name: "Dark Matter", image: darkmatterTier, minutes: "200h", description: "Ultimate transcendence", color: "#1a0033" }
		],
		album: [
			{ name: "Bronze", image: bronzeTier, minutes: "2h", description: "Your journey begins", color: "#CD7F32" },
			{ name: "Silver", image: silverTier, minutes: "4h", description: "Building dedication", color: "#C0C0C0" },
			{ name: "Peridot", image: peridotTier, minutes: "8h", description: "Growing passion", color: "#E6E200" },
			{ name: "Gold", image: goldTier, minutes: "15h", description: "True appreciation", color: "#FFD700" },
			{ name: "Emerald", image: emeraldTier, minutes: "30h", description: "Deep connection", color: "#50C878" },
			{ name: "Sapphire", image: sapphireTier, minutes: "60h", description: "Passionate listener", color: "#0F52BA" },
			{ name: "Garnet", image: garnetTier, minutes: "100h", description: "Unwavering loyalty", color: "#722F37" },
			{ name: "Jet", image: jetTier, minutes: "150h", description: "Extraordinary bond", color: "#343434" },
			{ name: "Diamond", image: diamondTier, minutes: "250h", description: "Exceptional taste", color: "#B9F2FF" },
			{ name: "Opal", image: opalTier, minutes: "500h", description: "Rare devotion", color: "#A8C3BC" },
			{ name: "Dark Matter", image: darkmatterTier, minutes: "750h", description: "Ultimate transcendence", color: "#1a0033" }
		],
		artist: [
			{ name: "Bronze", image: bronzeTier, minutes: "5h", description: "Your journey begins", color: "#CD7F32" },
			{ name: "Silver", image: silverTier, minutes: "10h", description: "Building dedication", color: "#C0C0C0" },
			{ name: "Peridot", image: peridotTier, minutes: "20h", description: "Growing passion", color: "#E6E200" },
			{ name: "Gold", image: goldTier, minutes: "40h", description: "True appreciation", color: "#FFD700" },
			{ name: "Emerald", image: emeraldTier, minutes: "75h", description: "Deep connection", color: "#50C878" },
			{ name: "Sapphire", image: sapphireTier, minutes: "150h", description: "Passionate listener", color: "#0F52BA" },
			{ name: "Garnet", image: garnetTier, minutes: "250h", description: "Unwavering loyalty", color: "#722F37" },
			{ name: "Jet", image: jetTier, minutes: "400h", description: "Extraordinary bond", color: "#343434" },
			{ name: "Diamond", image: diamondTier, minutes: "600h", description: "Exceptional taste", color: "#B9F2FF" },
			{ name: "Opal", image: opalTier, minutes: "1000h", description: "Rare devotion", color: "#A8C3BC" },
			{ name: "Dark Matter", image: darkmatterTier, minutes: "1500h", description: "Ultimate transcendence", color: "#1a0033" }
		]
	};

	const tiers = tierData[selectedType];

	return (
		<section id="tiers" className="relative py-20 md:py-32 bg-gradient-to-b from-[#0A0B0D] via-[#0A0B0D]/95 to-[#0A0B0D]">
			<div className="absolute inset-0 -z-10" aria-hidden>
				<div className="pointer-events-none absolute top-1/4 left-1/4 h-96 w-96 rounded-full bg-[#7C4DFF]/10 blur-3xl" />
				<div className="pointer-events-none absolute bottom-1/4 right-1/4 h-96 w-96 rounded-full bg-[#00E5C3]/5 blur-3xl" />
			</div>
			
			<div className="mx-auto max-w-7xl px-4">
				<div className="text-center mb-16">
					<h2 className="text-4xl md:text-6xl font-bold text-white mb-6">
						Unlock Your <span className="text-[#7C4DFF]">Prestige</span>
					</h2>
					<p className="text-xl text-zinc-300 max-w-3xl mx-auto mb-8">
						Every minute of listening earns you prestige. Climb through 11 unique tiers, each representing deeper dedication to your music.
					</p>

					{/* Dropdown Toggle */}
					<div className="flex justify-center">
						<div className="relative inline-block">
							<div className="flex bg-[#7C4DFF]/20 backdrop-blur-sm rounded-xl border border-[#7C4DFF]/30 p-1">
								<button
									onClick={() => setSelectedType('track')}
									className={`px-6 py-3 text-sm font-semibold transition-all duration-300 rounded-lg ${
										selectedType === 'track'
											? 'bg-[#7C4DFF] text-white shadow-lg shadow-[#7C4DFF]/30'
											: 'text-zinc-300 hover:text-white hover:bg-white/10'
									}`}
								>
									Tracks
								</button>
								<button
									onClick={() => setSelectedType('album')}
									className={`px-6 py-3 text-sm font-semibold transition-all duration-300 rounded-lg ${
										selectedType === 'album'
											? 'bg-[#7C4DFF] text-white shadow-lg shadow-[#7C4DFF]/30'
											: 'text-zinc-300 hover:text-white hover:bg-white/10'
									}`}
								>
									Albums
								</button>
								<button
									onClick={() => setSelectedType('artist')}
									className={`px-6 py-3 text-sm font-semibold transition-all duration-300 rounded-lg ${
										selectedType === 'artist'
											? 'bg-[#7C4DFF] text-white shadow-lg shadow-[#7C4DFF]/30'
											: 'text-zinc-300 hover:text-white hover:bg-white/10'
									}`}
								>
									Artists
								</button>
							</div>
						</div>
					</div>
				</div>

				{/* 6 on Top, 5 on Bottom Layout */}
				<div className="max-w-7xl mx-auto space-y-4 sm:space-y-6">
					{/* Top Row - 6 Tiers (Bronze to Sapphire) */}
					<div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3 sm:gap-4 md:gap-6">
						{tiers.slice(0, 6).map((tier, index) => (
							<div
								key={tier.name}
								className="group relative bg-white/5 backdrop-blur-sm rounded-xl sm:rounded-2xl p-3 sm:p-4 md:p-6 border border-white/10 hover:border-white/20 transition-all duration-500 hover:transform hover:-translate-y-2 hover:shadow-2xl hover:shadow-purple-500/10 flex flex-col h-44 sm:h-48 md:h-56"
								style={{
									animationDelay: `${index * 100}ms`
								}}
							>
								<div className="relative mb-2 sm:mb-3 md:mb-4">
									<img 
										src={tier.image} 
										alt={`${tier.name} tier`}
										className="w-10 h-10 sm:w-12 sm:h-12 md:w-16 md:h-16 mx-auto object-contain group-hover:scale-110 transition-transform duration-300"
									/>
									<div 
										className="absolute -inset-4 rounded-full opacity-0 group-hover:opacity-20 transition-opacity duration-300"
										style={{
											background: `radial-gradient(circle, ${tier.color}40, transparent)`
										}}
									/>
								</div>
								
								<div className="flex flex-col items-center text-center flex-grow justify-center">
									<h3 className="text-sm sm:text-base md:text-lg font-semibold text-white mb-1 sm:mb-2">{tier.name}</h3>
									<p className="text-xs sm:text-sm font-mono text-[#7C4DFF] mb-2 sm:mb-3">{tier.minutes}</p>
									<p className="text-xs sm:text-sm text-zinc-400 leading-relaxed text-center px-1">{tier.description}</p>
								</div>
							</div>
						))}
					</div>

					{/* Bottom Row - 5 Tiers (Garnet to Dark Matter) centered with flex + calc widths matching top grid */}
					<div className="flex justify-center">
						<div className="w-full max-w-7xl flex flex-wrap justify-center gap-3 sm:gap-4 md:gap-6">
							{tiers.slice(6).map((tier, index) => (
								<div
									key={tier.name}
									className="group relative bg-gradient-to-br from-white/10 to-white/5 backdrop-blur-sm rounded-xl sm:rounded-2xl p-3 sm:p-4 md:p-6 border border-white/20 hover:border-white/30 transition-all duration-500 hover:transform hover:-translate-y-3 hover:shadow-2xl flex flex-col h-44 sm:h-48 md:h-56 w-[calc((100%_-_0.75rem)/2)] sm:w-[calc((100%_-_2rem)/3)] md:w-[calc((100%_-_3rem)/4)] lg:w-[calc((100%_-_7.5rem)/6)]"
									style={{
										boxShadow: `0 0 20px ${tier.color}20`,
										animationDelay: `${(index + 6) * 100}ms`
									}}
								>
									<div className="relative mb-2 sm:mb-3 md:mb-4">
										<img 
											src={tier.image} 
											alt={`${tier.name} tier`}
											className="w-10 h-10 sm:w-12 sm:h-12 md:w-16 md:h-16 mx-auto object-contain group-hover:scale-110 transition-transform duration-300"
										/>
										<div 
											className="absolute -inset-4 rounded-full opacity-0 group-hover:opacity-30 transition-opacity duration-300"
											style={{
												background: `radial-gradient(circle, ${tier.color}60, transparent)`
											}}
										/>
									</div>
									
									<div className="flex flex-col items-center text-center flex-grow justify-center">
										<h3 className="text-sm sm:text-base md:text-lg font-semibold text-white mb-1 sm:mb-2">{tier.name}</h3>
										<p className="text-xs sm:text-sm font-mono text-[#9E7CFF] mb-2 sm:mb-3">{tier.minutes}</p>
										<p className="text-xs sm:text-sm text-zinc-400 leading-relaxed text-center px-1">{tier.description}</p>
									</div>
								</div>
							))}
						</div>
					</div>
				</div>

				<div className="text-center mt-16">
					<div className="inline-flex items-center gap-2 bg-white/5 backdrop-blur-sm rounded-full px-6 py-3 border border-white/10">
						<div className="w-2 h-2 bg-[#7C4DFF] rounded-full animate-pulse" />
						<span className="text-zinc-300">Start your prestige journey today</span>
					</div>
				</div>
			</div>
		</section>
	);
};

export default TierShowcase;
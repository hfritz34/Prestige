import React, { useState } from "react";
import bronzeTier from "@/assets/tiers/bronze.png";
import silverTier from "@/assets/tiers/silver.png";
import goldTier from "@/assets/tiers/gold.png";
import diamondTier from "@/assets/tiers/diamond.png";
import emeraldTier from "@/assets/tiers/emerald.png";
import sapphireTier from "@/assets/tiers/sapphire.png";
import rubyTier from "@/assets/tiers/ruby.png";
import amberTier from "@/assets/tiers/amber.png";
import jadeTier from "@/assets/tiers/jade.png";
import amethystTier from "@/assets/tiers/amethyst.png";
import opalTier from "@/assets/tiers/opal.png";
import peridotTier from "@/assets/tiers/peridot.png";
import jetTier from "@/assets/tiers/jet.png";
import darkmatterTier from "@/assets/tiers/darkmatter.png";
import cosmicTier from "@/assets/tiers/cosmic.png";

type TierType = 'track' | 'album' | 'artist';

const TierShowcase: React.FC = () => {
	const [selectedType, setSelectedType] = useState<TierType>('track');
	
	const tierData = {
		track: [
			{ name: "Bronze", image: bronzeTier, minutes: "10m", description: "Your journey begins", color: "#CD7F32" },
			{ name: "Silver", image: silverTier, minutes: "20m", description: "Building dedication", color: "#C0C0C0" },
			{ name: "Peridot", image: peridotTier, minutes: "35m", description: "Growing passion", color: "#9ACD32" },
			{ name: "Gold", image: goldTier, minutes: "1h", description: "True appreciation", color: "#FFD700" },
			{ name: "Emerald", image: emeraldTier, minutes: "2h", description: "Deep connection", color: "#50C878" },
			{ name: "Sapphire", image: sapphireTier, minutes: "3h", description: "Passionate listener", color: "#0F52BA" },
			{ name: "Ruby", image: rubyTier, minutes: "4h", description: "Fiery dedication", color: "#E0115F" },
			{ name: "Amber", image: amberTier, minutes: "5h", description: "Unwavering loyalty", color: "#722F37" },
			{ name: "Jade", image: jadeTier, minutes: "8h", description: "Precious devotion", color: "#00A86B" },
			{ name: "Amethyst", image: amethystTier, minutes: "11h", description: "Mystical bond", color: "#9966CC" },
			{ name: "Jet", image: jetTier, minutes: "15h", description: "Extraordinary bond", color: "#343434" },
			{ name: "Diamond", image: diamondTier, minutes: "22h", description: "Exceptional taste", color: "#B9F2FF" },
			{ name: "Opal", image: opalTier, minutes: "30h", description: "Rare devotion", color: "#FFEFDB" },
			{ name: "Dark Matter", image: darkmatterTier, minutes: "40h", description: "Ultimate transcendence", color: "#301934" },
			{ name: "Cosmic", image: cosmicTier, minutes: "50h", description: "Beyond mortal limits", color: "#4B0082" }
		],
		album: [
			{ name: "Bronze", image: bronzeTier, minutes: "30m", description: "Your journey begins", color: "#CD7F32" },
			{ name: "Silver", image: silverTier, minutes: "1h", description: "Building dedication", color: "#C0C0C0" },
			{ name: "Peridot", image: peridotTier, minutes: "2h", description: "Growing passion", color: "#9ACD32" },
			{ name: "Gold", image: goldTier, minutes: "3h", description: "True appreciation", color: "#FFD700" },
			{ name: "Emerald", image: emeraldTier, minutes: "6h", description: "Deep connection", color: "#50C878" },
			{ name: "Sapphire", image: sapphireTier, minutes: "9h", description: "Passionate listener", color: "#0F52BA" },
			{ name: "Ruby", image: rubyTier, minutes: "13h", description: "Fiery dedication", color: "#E0115F" },
			{ name: "Amber", image: amberTier, minutes: "20h", description: "Unwavering loyalty", color: "#722F37" },
			{ name: "Jade", image: jadeTier, minutes: "28h", description: "Precious devotion", color: "#00A86B" },
			{ name: "Amethyst", image: amethystTier, minutes: "40h", description: "Mystical bond", color: "#9966CC" },
			{ name: "Jet", image: jetTier, minutes: "57h", description: "Extraordinary bond", color: "#343434" },
			{ name: "Diamond", image: diamondTier, minutes: "80h", description: "Exceptional taste", color: "#B9F2FF" },
			{ name: "Opal", image: opalTier, minutes: "108h", description: "Rare devotion", color: "#FFEFDB" },
			{ name: "Dark Matter", image: darkmatterTier, minutes: "167h", description: "Ultimate transcendence", color: "#301934" },
			{ name: "Cosmic", image: cosmicTier, minutes: "200h", description: "Beyond mortal limits", color: "#4B0082" }
		],
		artist: [
			{ name: "Bronze", image: bronzeTier, minutes: "1h", description: "Your journey begins", color: "#CD7F32" },
			{ name: "Silver", image: silverTier, minutes: "2h", description: "Building dedication", color: "#C0C0C0" },
			{ name: "Peridot", image: peridotTier, minutes: "3h", description: "Growing passion", color: "#9ACD32" },
			{ name: "Gold", image: goldTier, minutes: "6h", description: "True appreciation", color: "#FFD700" },
			{ name: "Emerald", image: emeraldTier, minutes: "9h", description: "Deep connection", color: "#50C878" },
			{ name: "Sapphire", image: sapphireTier, minutes: "14h", description: "Passionate listener", color: "#0F52BA" },
			{ name: "Ruby", image: rubyTier, minutes: "22h", description: "Fiery dedication", color: "#E0115F" },
			{ name: "Amber", image: amberTier, minutes: "32h", description: "Unwavering loyalty", color: "#722F37" },
			{ name: "Jade", image: jadeTier, minutes: "45h", description: "Precious devotion", color: "#00A86B" },
			{ name: "Amethyst", image: amethystTier, minutes: "62h", description: "Mystical bond", color: "#9966CC" },
			{ name: "Jet", image: jetTier, minutes: "83h", description: "Extraordinary bond", color: "#343434" },
			{ name: "Diamond", image: diamondTier, minutes: "108h", description: "Exceptional taste", color: "#B9F2FF" },
			{ name: "Opal", image: opalTier, minutes: "142h", description: "Rare devotion", color: "#FFEFDB" },
			{ name: "Dark Matter", image: darkmatterTier, minutes: "200h", description: "Ultimate transcendence", color: "#301934" },
			{ name: "Cosmic", image: cosmicTier, minutes: "250h", description: "Beyond mortal limits", color: "#4B0082" }
		]
	};

	const tiers = tierData[selectedType];

	return (
		<section id="tiers" className="relative py-20 md:py-32 bg-gradient-to-b from-[#0A0B0D] via-[#0A0B0D]/95 to-[#0A0B0D]">
			<div className="absolute inset-0 -z-10" aria-hidden>
				<div className="pointer-events-none absolute top-1/4 left-1/4 h-96 w-96 rounded-full bg-[#7C4DFF]/10 blur-3xl" />
				<div className="pointer-events-none absolute bottom-1/4 right-1/4 h-96 w-96 rounded-full bg-[#9E7CFF]/5 blur-3xl" />
			</div>
			
			<div className="mx-auto max-w-7xl px-4">
				<div className="text-center mb-16">
					<h2 className="text-4xl md:text-6xl font-bold text-white mb-6">
						Unlock Your <span className="text-[#7C4DFF]">Prestige</span>
					</h2>
					<p className="text-xl text-zinc-300 max-w-3xl mx-auto mb-8">
						Every minute of listening earns you prestige. Climb through 15 unique tiers, each representing deeper dedication to your music.
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

				{/* Clean Grid Layout - 5 rows x 3 cols on mobile, 3 cols x 5 rows on desktop */}
				<div className="max-w-5xl mx-auto">
					<div className="grid grid-cols-3 md:grid-cols-3 gap-3 md:gap-6">
						{tiers.map((tier, index) => (
							<div
								key={tier.name}
								className="group relative bg-white/5 backdrop-blur-sm rounded-lg md:rounded-xl p-2 md:p-4 border border-white/10 hover:border-white/20 transition-all duration-500 hover:transform hover:-translate-y-1 hover:shadow-2xl flex flex-col aspect-square"
								style={{
									boxShadow: `0 0 20px ${tier.color}15`,
									animationDelay: `${index * 80}ms`
								}}
							>
								<div className="relative flex-grow flex items-center justify-center mb-1 md:mb-2">
									<img 
										src={tier.image} 
										alt={`${tier.name} tier`}
										className="w-16 h-16 md:w-28 md:h-28 object-contain group-hover:scale-110 transition-transform duration-300"
									/>
									<div 
										className="absolute -inset-3 md:-inset-6 rounded-full opacity-0 group-hover:opacity-25 transition-opacity duration-300"
										style={{
											background: `radial-gradient(circle, ${tier.color}40, transparent)`
										}}
									/>
								</div>
								
								<div className="flex flex-col items-center text-center flex-shrink-0">
									<h3 className="text-xs md:text-lg font-semibold text-white mb-0.5 md:mb-1">{tier.name}</h3>
									<p className="text-xs md:text-sm font-mono text-[#7C4DFF] mb-0.5 md:mb-1">{tier.minutes}</p>
									<p className="text-xs md:text-xs text-zinc-400 leading-tight px-0.5 md:px-1 hidden sm:block">{tier.description}</p>
								</div>
							</div>
						))}
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
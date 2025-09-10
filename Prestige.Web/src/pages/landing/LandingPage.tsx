import React from "react";
import crownLogo from "@/assets/white_logo.png";
import tierProgress from "@/assets/tierprog.png";
import TierShowcase from "./components/tiers/TierShowcase";
import InteractiveRankingDemo from "./components/InteractiveRankingDemo";

const LandingPage: React.FC = () => {
	// Landing page deployment test
	return (
		<div className="min-h-screen w-full bg-[#0A0B0D] text-white overflow-x-hidden">
			{/* Top Navigation */}
			<header className="w-full sticky top-0 z-40 backdrop-blur supports-[backdrop-filter]:bg-black/30 bg-black/40">
				<div className="w-full px-4 py-2 relative flex items-center">
					<div className="flex items-center">
						<a
							href="#"
							aria-label="Scroll to top"
							onClick={(e) => {
								e.preventDefault();
								window.scrollTo({ top: 0, behavior: "smooth" });
							}}
							className="p-1"
						>
							<img src={crownLogo} alt="Prestige crown" className="h-8 w-8 object-contain" />
						</a>
					</div>
					
					{/* Absolutely centered navigation */}
					<nav className="hidden md:flex items-center gap-8 text-sm text-zinc-300 absolute left-1/2 transform -translate-x-1/2">
						<a href="#tiers" className="hover:text-white">How it works</a>
						<a href="#features" className="hover:text-white">Features</a>
						<a href="#community" className="hover:text-white">Community</a>
						<a href="#faq" className="hover:text-white">FAQ</a>
					</nav>
					
					<div className="flex items-center gap-3 ml-auto">
						<a href="#waitlist" className="rounded-xl px-4 py-2 text-sm font-medium bg-[#7C4DFF] hover:bg-[#6b3bff] transition-all duration-300 transform hover:scale-[1.02] shadow-md hover:shadow-lg">Join Beta</a>
					</div>
				</div>
			</header>

			{/* Hero */}
			<section className="relative overflow-hidden min-h-screen flex items-center justify-center">
				<div className="absolute inset-0 -z-10" aria-hidden>
					<div className="pointer-events-none absolute -top-40 right-1/4 h-96 w-96 rounded-full bg-[#7C4DFF]/20 blur-3xl" />
					<div className="pointer-events-none absolute -bottom-40 left-1/3 h-[28rem] w-[28rem] rounded-full bg-[#9E7CFF]/10 blur-3xl" />
				</div>
				<div className="w-full px-4 text-center">
					<h1 className="text-6xl sm:text-6xl md:text-8xl lg:text-9xl font-bold tracking-tight leading-tight">
						<span className="block sm:inline">Turn your</span>
						<span className="block sm:inline"> music into</span><br className="hidden sm:block" />
						<span className="text-[#7C4DFF] prestige-glow">Prestige</span>
					</h1>
					<p className="mt-12 sm:mt-6 md:mt-8 text-lg sm:text-xl md:text-2xl text-zinc-300 max-w-3xl mx-auto leading-relaxed px-2">
						Earn time-based badges, rate your taste, and compare with friends. Your music life—measured, celebrated, and made social.
					</p>
					<div className="mt-8 md:mt-12 flex flex-col sm:flex-row items-center justify-center gap-3 sm:gap-4 max-w-md sm:max-w-none mx-auto">
						<a href="#waitlist" className="w-full sm:w-auto rounded-2xl px-8 sm:px-12 py-3 sm:py-4 text-base sm:text-lg font-semibold bg-[#7C4DFF] hover:bg-[#6b3bff] text-white transition-all duration-300 transform hover:scale-[1.02] shadow-md hover:shadow-lg">
							Join the Beta
						</a>
					</div>
					<p className="mt-4 md:mt-6 text-sm text-zinc-400 px-4">Requires Spotify account. Private by default—share what you choose.</p>
				</div>
			</section>

			{/* Tier Progression Showcase */}
			<TierShowcase />

			{/* How Prestige Works */}
			<section id="how" className="w-full px-4 py-16 md:py-24 flex justify-center">
				<div className="max-w-7xl w-full">
					<div className="grid md:grid-cols-2 gap-12 items-center">
						<div>
							<h2 className="text-2xl md:text-3xl font-semibold">Level up your <strong className="text-[#7C4DFF] font-bold">Prestige</strong>. Collect 16+ tiers.<br /> Flex your dedication.</h2>
							
							<p className="mt-4 text-zinc-300">
								Every minute you listen levels up your tracks, albums, and artists. Unlock and collect unique tier badges—from Bronze to Prestige—that showcase how devoted you are.
							</p>
							<ul className="mt-6 space-y-2 text-sm text-zinc-400 list-disc list-inside">
								<li>Progress separately for tracks, albums, and artists—build complete sets across your library.</li>
								<li>16 collectible tiers per item: Bronze → Silver → Gold → … → Prestige.</li>
								<li>Show them off on your profile and compare with friends.</li>
							</ul>
						</div>
						<div className="relative">
							<img src={tierProgress} alt="Prestige tiers progression" className="w-full rounded-xl shadow-xl shadow-black/40" />
						</div>
					</div>
				</div>
			</section>

			{/* Rating System Preview */}
			<section className="w-full px-4 py-16 md:py-24 flex justify-center">
				<div className="max-w-7xl w-full">
					<div className="text-center mb-12">
						<h2 className="text-3xl md:text-5xl font-bold text-white mb-4">
							Rate Your <span className="text-[#7C4DFF]">Taste</span>
						</h2>
						<p className="text-xl text-zinc-300 max-w-2xl mx-auto">
							Build your personal music rankings through intelligent comparisons. Every choice shapes your unique taste profile.
						</p>
					</div>
					
					<div className="max-w-5xl mx-auto bg-white/5 backdrop-blur-sm rounded-2xl border border-white/10">
						<InteractiveRankingDemo />
					</div>
				</div>
			</section>

			{/* Features */}
			<section id="features" className="w-full px-4 py-12 md:py-20 flex justify-center">
				<div className="max-w-7xl w-full">
					<div className="grid md:grid-cols-3 gap-6">
						<div className="rounded-xl bg-white/5 border border-white/10 p-6 hover:bg-white/10 transition-all duration-300">
							<h3 className="text-lg font-semibold">Earn Prestige Over Time</h3>
							<p className="mt-2 text-sm text-zinc-300"><strong>Real-time tracking</strong> from 1 hour to 500+ hours per song. Unlock Bronze through Prestige tiers as your dedication grows. Your listening creates lasting achievements.</p>
						</div>
						<div className="rounded-xl bg-white/5 border border-white/10 p-6 hover:bg-white/10 transition-all duration-300">
							<h3 className="text-lg font-semibold">Complete Listening History</h3>
							<p className="mt-2 text-sm text-zinc-300"><strong>Beyond Spotify's limits</strong> - access your full listening history with live Now Playing. See exactly when you discovered songs and track your music journey over years.</p>
						</div>
						<div className="rounded-xl bg-white/5 border border-white/10 p-6 hover:bg-white/10 transition-all duration-300">
							<h3 className="text-lg font-semibold">Private by Default</h3>
							<p className="mt-2 text-sm text-zinc-300"><strong>You control sharing</strong> - keep your data private or selectively share with friends. Compare prestige levels, discover music through others' dedication, all on your terms.</p>
						</div>
					</div>
				</div>
			</section>

			{/* Community CTA */}


			{/* Waitlist Section */}
			<section id="waitlist" className="w-full px-4 py-16 bg-gradient-to-b from-[#0A0B0D] to-[#7C4DFF]/10 flex justify-center">
				<div className="max-w-3xl w-full text-center">
					<h2 className="text-3xl md:text-4xl font-bold text-white mb-4">
						Be First to Experience <span className="text-[#7C4DFF]">Prestige</span>
					</h2>
					<p className="text-xl text-zinc-300 mb-8 leading-relaxed">
						Join our beta to get early access. Limited spots available for beta testing.
					</p>
					
					<div className="max-w-md mx-auto">
						<form 
							action="https://formspree.io/f/xvgbkzbn" 
							method="POST"
							className="flex flex-col sm:flex-row gap-4"
						>
							<input
								type="email"
								name="email"
								required
								placeholder="Enter your email address"
								className="flex-1 px-4 py-3 rounded-lg bg-white/10 border border-white/20 text-white placeholder-zinc-400 focus:outline-none focus:ring-2 focus:ring-[#7C4DFF] focus:border-transparent"
							/>
							<button
								type="submit"
								className="px-8 py-3 bg-[#7C4DFF] hover:bg-[#6b3bff] text-white font-semibold rounded-lg transition-colors duration-300"
							>
								Join Beta
							</button>
						</form>
						<p className="text-xs text-zinc-500 mt-4">
							We'll never spam you. Unsubscribe at any time.
						</p>
					</div>
				</div>
			</section>

			{/* Footer */}
			<footer id="faq" className="border-t border-white/10">
				<div className="w-full px-4 py-10 flex justify-center">
					<div className="max-w-7xl w-full grid md:grid-cols-3 gap-8 text-sm text-zinc-400">
						<div className="space-y-3">
							<div className="flex items-center gap-2">
								<img src={crownLogo} alt="Prestige crown" className="h-5 w-5 object-contain" />
								<span className="text-white">Prestige</span>
							</div>
							<p>Turn your listening into Prestige. Private by default—share what you choose.</p>
						</div>
						<div>
							<p className="text-white mb-3">FAQ</p>
							<ul className="space-y-2">
								<li>What is a prestige? Minutes listened → tier badges.</li>
								<li>How do I earn prestige? Listen more; thresholds vary by item type.</li>
								<li>Can I compare with friends? Yes—per item and on profiles.</li>
							</ul>
						</div>
						<div>
							<p className="text-white mb-3">Get notified</p>
							<a href="#waitlist" className="inline-block rounded-md px-4 py-2 bg-[#7C4DFF] hover:bg-[#6b3bff] transition-colors text-white">Join Beta</a>
						</div>
					</div>
				</div>
				<div className="text-center text-xs text-zinc-500 pb-8">© {new Date().getFullYear()} Prestige</div>
			</footer>
		</div>
	);
};

export default LandingPage;
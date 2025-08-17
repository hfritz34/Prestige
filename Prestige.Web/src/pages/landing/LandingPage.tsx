import React from "react";
import { Link } from "react-router-dom";
import crownLogo from "@/assets/purplecrowntrans.png";
import tierProgress from "@/assets/tierprog.png";

const LandingPage: React.FC = () => {
	return (
		<div className="min-h-screen w-full bg-[#0A0B0D] text-white">
			{/* Top Navigation */}
			<header className="w-full sticky top-0 z-40 backdrop-blur supports-[backdrop-filter]:bg-black/30 bg-black/40">
				<div className="mx-auto max-w-7xl px-4 py-4 flex items-center justify-between">
					<div className="flex items-center gap-3">
						<a
							href="#"
							aria-label="Scroll to top"
							onClick={(e) => {
								e.preventDefault();
								window.scrollTo({ top: 0, behavior: "smooth" });
							}}
						>
							<img src={crownLogo} alt="Prestige crown" className="h-8 w-8" />
						</a>
					</div>
					<nav className="hidden md:flex items-center gap-8 text-sm text-zinc-300">
						<a href="#how" className="hover:text-white">How it works</a>
						<a href="#features" className="hover:text-white">Features</a>
						<a href="#community" className="hover:text-white">Community</a>
						<a href="#faq" className="hover:text-white">FAQ</a>
					</nav>
					<div className="flex items-center gap-3">
						<Link to="/sign-in" className="rounded-md px-4 py-2 text-sm font-medium bg-[#7C4DFF] hover:bg-[#6b3bff] transition-colors">Open Web App</Link>
						<a href="#download" className="rounded-md px-4 py-2 text-sm font-medium bg-[#9E7CFF] text-black hover:bg-[#8B5CFF] transition-colors">Download on iOS</a>
					</div>
				</div>
			</header>

			{/* Hero */}
			<section className="relative overflow-hidden border-b border-white/5">
				<div className="absolute inset-0 -z-10" aria-hidden>
					<div className="pointer-events-none absolute -top-40 right-1/4 h-96 w-96 rounded-full bg-[#7C4DFF]/20 blur-3xl" />
					<div className="pointer-events-none absolute -bottom-40 left-1/3 h-[28rem] w-[28rem] rounded-full bg-[#00E5C3]/10 blur-3xl" />
				</div>
				<div className="mx-auto max-w-4xl px-4 py-24 md:py-32 text-center">
					<h1 className="text-5xl md:text-7xl font-semibold tracking-tight">
						Turn your listening into <span className="text-[#7C4DFF]">Prestige</span>
					</h1>
					<p className="mt-6 text-xl text-zinc-300">
						Earn time-based badges, rate your taste, and compare with friends. Your music life—measured, celebrated, and made social.
					</p>
					<div className="mt-9 flex items-center justify-center gap-3">
						<a href="#download" className="rounded-md px-6 py-3 text-sm font-semibold bg-[#9E7CFF] text-black hover:bg-[#8B5CFF] transition-colors">Download on iOS</a>
						<Link to="/sign-in" className="rounded-md px-6 py-3 text-sm font-semibold bg-white/10 hover:bg-white/20 transition-colors">Open Web App</Link>
					</div>
					<p className="mt-4 text-xs text-zinc-400">Requires Spotify account. Private by default—share what you choose.</p>
				</div>
			</section>

			{/* How Prestige Works */}
			<section id="how" className="mx-auto max-w-7xl px-4 py-16 md:py-24">
				<div className="grid md:grid-cols-2 gap-12 items-center">
					<div>
						<h2 className="text-2xl md:text-3xl font-semibold">Level up your Prestige with your listening time</h2>
						<p className="mt-4 text-zinc-300">
							Prestige is earned by minutes listened per track, album, and artist. Hit thresholds to unlock tiers—from Bronze to Dark Matter.
						</p>
						<ul className="mt-6 space-y-2 text-sm text-zinc-400 list-disc list-inside">
							<li>Tracks: Bronze ≥ 60m, Silver ≥ 150m, Gold ≥ 500m, … DarkMatter ≥ 15,000m</li>
							<li>Albums and Artists: higher thresholds to reflect bigger commitments</li>
						</ul>
					</div>
					<div className="relative">
						<img src={tierProgress} alt="Prestige tiers progression" className="w-full rounded-xl border border-white/10 shadow-xl shadow-black/40" />
					</div>
				</div>
			</section>

			{/* Features */}
			<section id="features" className="mx-auto max-w-7xl px-4 py-12 md:py-20">
				<div className="grid md:grid-cols-3 gap-6">
					<div className="rounded-xl bg-white/5 border border-white/10 p-6">
						<h3 className="text-lg font-semibold">Rate your taste</h3>
						<p className="mt-2 text-sm text-zinc-300">Loved / Liked / Disliked. Rank within lists and build your personal score.</p>
					</div>
					<div className="rounded-xl bg-white/5 border border-white/10 p-6">
						<h3 className="text-lg font-semibold">Compare with friends</h3>
						<p className="mt-2 text-sm text-zinc-300">See who grinds harder for your favorite tracks, albums, and artists.</p>
					</div>
					<div className="rounded-xl bg-white/5 border border-white/10 p-6">
						<h3 className="text-lg font-semibold">Live + history</h3>
						<p className="mt-2 text-sm text-zinc-300">Now Playing and full listening history beyond Spotify’s limits.</p>
					</div>
				</div>
			</section>

			{/* Community CTA */}
			<section id="community" className="mx-auto max-w-7xl px-4 pb-20">
				<div className="rounded-2xl border border-white/10 bg-gradient-to-br from-[#7C4DFF]/20 to-transparent p-8 md:p-12 flex flex-col md:flex-row items-center justify-between gap-6">
					<div>
						<h3 className="text-2xl font-semibold">Join the Prestige community</h3>
						<p className="mt-2 text-zinc-300">Compete with friends. Discover new music through dedication and taste.</p>
					</div>
					<div className="flex items-center gap-3">
						<Link to="/sign-in" className="rounded-md px-5 py-3 text-sm font-semibold bg-white/10 hover:bg-white/20 transition-colors">Open Web App</Link>
						<a href="#download" className="rounded-md px-5 py-3 text-sm font-semibold bg-[#9E7CFF] text-black hover:bg-[#8B5CFF] transition-colors">Download on iOS</a>
					</div>
				</div>
			</section>

			{/* Footer */}
			<footer id="faq" className="border-t border-white/10">
				<div className="mx-auto max-w-7xl px-4 py-10 grid md:grid-cols-3 gap-8 text-sm text-zinc-400">
					<div className="space-y-3">
						<div className="flex items-center gap-2">
							<img src={crownLogo} alt="Prestige crown" className="h-5 w-5" />
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
						<p className="text-white mb-3">Get the app</p>
						<a id="download" href="#" className="inline-block rounded-md px-4 py-2 bg-[#9E7CFF] text-black hover:bg-[#8B5CFF] transition-colors">Download on iOS</a>
					</div>
				</div>
				<div className="text-center text-xs text-zinc-500 pb-8">© {new Date().getFullYear()} Prestige</div>
			</footer>
		</div>
	);
};

export default LandingPage;
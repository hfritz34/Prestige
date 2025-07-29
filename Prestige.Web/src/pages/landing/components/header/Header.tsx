// src/pages/landing/components/header/Header.tsx
import { useEffect } from 'react';
import { ChevronDown, Ellipsis } from 'lucide-react';
import PrestigeWordmarkWhite from '@/assets/prestige_wordmark_white.png';

const Header: React.FC = () => {
    useEffect(() => {
        const handleScroll = () => {
            const opacity = Math.min(window.scrollY / 100, 1);
            document.documentElement.style.setProperty(
                '--header-opacity',
                opacity.toString()
            );
        };

        window.addEventListener('scroll', handleScroll);
        return () => window.removeEventListener('scroll', handleScroll);
    }, []);

    return (
        <div
            className={"fixed z-[1000] top-0 left-0 right-0 h-16 flex items-center px-5 justify-between bg-[oklch(0.5769_0.2357_278.44_/_var(--header-opacity))]"}
        >
            <ChevronDown className="size-8 text-spotify-white stroke-1" />
            <img
                src={PrestigeWordmarkWhite}
                alt="Prestige Wordmark White Color"
                className="h-7 -translate-y-0.5"
            />
            <Ellipsis className="size-8 stroke-1 text-spotify-white fill-spotify-white" />
        </div>
    );
};

export default Header;
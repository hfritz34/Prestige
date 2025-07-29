import Hero from "@/pages/landing/components/hero/Hero.tsx";
import Header from "./components/header/Header";
import Info from "./components/info/Info";

const LandingPage : React.FC = () => {
    return (
      <div className={"relative h-full w-full"}>
          <Header />
          <Hero />
          <div className={"w-full bg-spotify-black flex flex-col items-center"}>
              <Info />
          </div>
      </div>
    );
}

export default LandingPage;
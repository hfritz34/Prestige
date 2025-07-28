import {Play, Shuffle, Repeat, SkipBack, SkipForward, PlusCircle} from "lucide-react";
import LogoWhiteBlue from "@/assets/logo_white_blue.png";

const Hero : React.FC = () => {
    return (
      <div className={`relative -z-20 flex flex-col gap-14 items-center w-full h-full mask-radial-at-bottom  bg-gradient-to-b from-80% to-100% from-spotify-black/40 to-spotify-black`}>
          <div className={"-z-10 absolute inset-0 bg-prestige-background mix-blend-darken"}></div>
            <div className={"z-10 text-spotify-green shrink-0 mt-4 h-8"}></div>
            <img src={LogoWhiteBlue} alt={"White angular triangle faceted crown on blue background"} className={"justify-self-center aspect-square w-11/12 rounded-lg drop-shadow-lg"}></img>
            <div className={`justify-self-end text-white h-full w-full flex flex-col items-center gap-8`}>
                <div className={"flex w-10/12 justify-between items-center"}>
                    <div className={"text-left flex flex-col justify-center"}>
                        <span className={"text-lg"}>Prestige</span>
                        <span className={"text-sm"}>Available on iOS</span>
                    </div>
                    <PlusCircle className={"size-9"} />
                </div>
                <div className={"relative bg-gray-800 h-1 w-10/12 rounded-full"}>
                    <div className={"absolute inset-0 bg-white w-4/6 rounded-full"}>
                        <div className={"absolute right-0 -top-1/2 size-2 rounded-full bg-white"}></div>
                    </div>
                </div>
                <div className={"w-10/12 flex justify-between items-center"}>
                    <Shuffle />
                    <SkipBack className={"fill-white size-8"}/>
                    <div className={"size-16 p-2 pl-[0.7rem] bg-spotify-white flex justify-center items-center rounded-full"}>
                        <Play className={"fill-prestige-background stroke-spotify-white size-10"}/>
                    </div>
                    <SkipForward className={"fill-white size-8"}/>
                    <Repeat />

                </div>
            </div>
      </div>
    );
}

export default Hero;
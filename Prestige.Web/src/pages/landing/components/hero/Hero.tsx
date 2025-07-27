import {Play, SkipBack, SkipForward} from "lucide-react";
import LogoWhiteBlue from "@/assets/logo_white_blue.png";

const Hero : React.FC = () => {
    return (
      <div className={`flex flex-col gap-8 items-center bg-spotify-black w-full h-full`}>
            <div className={"text-spotify-green shrink-0 mt-4"}>fasdfdas</div>
            <img src={LogoWhiteBlue} alt={"White angular triangle faceted crown on blue background"} className={"justify-self-center aspect-square w-11/12 rounded-lg drop-shadow-2xl"}></img>
            <div className={`justify-self-end text-white h-full w-full flex flex-col items-center gap-4`}>
                <div className={"text-left w-10/12 flex flex-col justify-center"}>
                    <span className={"text-lg"}>Prestige</span>
                    <span className={"text-sm"}>Available on iOS</span>
                </div>
                <div className={"relative bg-gray-800 h-1 w-10/12 rounded-full"}>
                    <div className={"absolute inset-0 bg-white w-4/6 rounded-full"}>
                        <div className={"absolute right-0 -top-1/2 size-2 rounded-full bg-white"}></div>
                    </div>
                </div>
                <div className={"w-10/12 flex justify-between items-center"}>
                    <SkipBack className={"fill-white"}/>
                    <div className={"size-12 p-2 pl-2.5 bg-spotify-green flex justify-center items-center rounded-full"}>
                        <Play className={"fill-spotify-black stroke-spotify-green size-7"}/>
                    </div>
                    <SkipForward className={"fill-white"}/>

                </div>
            </div>
      </div>
    );
}

export default Hero;
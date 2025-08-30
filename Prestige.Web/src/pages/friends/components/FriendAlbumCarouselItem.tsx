import { useState } from "react";
import { CarouselItem } from "@/components/ui/carousel";
import { UserAlbumResponse } from "@/hooks/useProfile";
import FriendItemModal from "@/components/ui/friend-item-modal";

type FriendAlbumCarouselItemProps = {
    userAlbum: UserAlbumResponse;
    friendNickname: string;
};

const FriendAlbumCarouselItem: React.FC<FriendAlbumCarouselItemProps> = ({ userAlbum, friendNickname }) => {
    const [showModal, setShowModal] = useState(false);

    const handleAlbumClick = () => {
        setShowModal(true);
    };

    return (
        <>
            <CarouselItem key={userAlbum.album.id} className="basis-1/3">
                <div className="album-item cursor-pointer hover:opacity-80 transition-opacity" onClick={handleAlbumClick}>
                    <img 
                        src={userAlbum.album.images[0]?.url || "/placeholder-album.png"} 
                        alt={userAlbum.album.name}
                        className="w-full aspect-square object-cover rounded-lg mb-2"
                    />
                    <h3 className="text-sm font-bold line-clamp-2">{userAlbum.album.name}</h3>
                    <div className="text-xs text-gray-400 line-clamp-1">
                        {userAlbum.album.artists.map(artist => artist.name).join(', ')}
                    </div>
                    <p className="text-xs text-gray-500 mt-1">{Math.floor(userAlbum.totalTime / 60)} min</p>
                </div>
            </CarouselItem>
            
            <FriendItemModal
                isOpen={showModal}
                onClose={() => setShowModal(false)}
                item={userAlbum}
                itemType="album"
                friendNickname={friendNickname}
            />
        </>
    );
};

export default FriendAlbumCarouselItem;